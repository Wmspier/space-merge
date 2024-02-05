using System.Collections.Generic;
using System.Linq;
using Hex.Data;
using Hex.Extensions;
using Hex.Grid.DetailQueue;
using Hex.Model;
using Hex.UI;
using UnityEngine;
using UnityEngine.Pool;

namespace Hex.Managers
{
	public class PlayerUnitManager : MonoBehaviour
	{
		[SerializeField] private List<UnitData> startingDeck;
		[SerializeField] private int startingHandSize = 4;
		
		[SerializeField] private DeckPreviewQueue deckPreviewQueue;
		[SerializeField] private GameUI gameUI;

		private BattleModel _battleModel;
		
		private void Awake()
		{
			deckPreviewQueue.DetailDequeued = OnDetailDequeued;
		}

		public void Initialize()
		{
			_battleModel = ApplicationManager.GetResource<BattleModel>();
			
			FillInitialDeck();
			FillHand();
			SetupPreviewQueue();
		}

		public void Dispose()
		{
			_battleModel.Deck.Clear();
			_battleModel.Hand.Clear();
			_battleModel.Discard.Clear();
			
			deckPreviewQueue.gameObject.SetActive(false);
			gameUI.PreviewQueueUI.gameObject.SetActive(false);
		}
		
		public void SetupPreviewQueue()
		{
			deckPreviewQueue.Initialize(GetUnitFromHand, _battleModel.Hand.Count);
			deckPreviewQueue.GeneratePreviewQueue();
			deckPreviewQueue.gameObject.SetActive(true);
            
			gameUI.PreviewQueueUI.Initialize(_battleModel.Hand.FirstOrDefault(), _battleModel.Hand.Count);
			gameUI.PreviewQueueUI.gameObject.SetActive(true);
		}
		
		private void FillInitialDeck()
		{
			foreach (var detail in startingDeck)
			{
				_battleModel.Deck.Add(detail);
			}
			_battleModel.Deck.Shuffle();
		}
		
		private void FillHand()
		{
			for (var i = 0; i < startingHandSize; i++)
			{
				if (_battleModel.Deck.Count == 0)
				{
					ShuffleDiscardIntoDeck();
					if (_battleModel.Deck.Count == 0) return;
				}
                
				_battleModel.Hand.Add(_battleModel.Deck[0]);
				_battleModel.Deck.RemoveAt(0);
			}
		}
		
		private void ShuffleDiscardIntoDeck()
		{
			_battleModel.Deck.AddRange(_battleModel.Discard);
			_battleModel.Deck.Shuffle();
			_battleModel.Discard.Clear();
		}
		
		private UnitData GetUnitFromHand(int index) => index < _battleModel.Hand.Count ? _battleModel.Hand[index] : null;

		private void OnDetailDequeued()
		{
			gameUI.PreviewQueueUI.SetNextAndDecrement(_battleModel.Hand.FirstOrDefault(), _battleModel.Hand.Count);
		}
		
		public void DrawNewHand()
		{
			_battleModel.Discard.AddRange(_battleModel.Hand);
			_battleModel.Hand.Clear();
			FillHand();
			SetupPreviewQueue();
		}

		public UnitData DrawNextUnit()
		{
			if (_battleModel.Hand.Count == 0)
			{
				Debug.LogError("Trying to draw on empty hand");
				return null;
			}
			
			// Draw next unit from hand then discard it
			var unit = _battleModel.Hand[0];
			_battleModel.Discard.Add(unit);
			_battleModel.Hand.RemoveAt(0);
			
			// Try to create a snapshot of the next details in the queue so they can be asynchronously processed
			var queueSnapshot = ListPool<UnitData>.Get();
			queueSnapshot.Clear();
			UnitData unitPreview;
			var index = 0;
			do
			{
				unitPreview = GetUnitFromHand(index);
				if (unitPreview != null) queueSnapshot.Add(unitPreview);
				index++;
			} while (unitPreview != null);
			deckPreviewQueue.Dequeue(queueSnapshot);

			return unit;
		}
	}
}