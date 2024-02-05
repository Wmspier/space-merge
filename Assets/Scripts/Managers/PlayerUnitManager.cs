using System.Collections.Generic;
using System.Linq;
using Hex.Data;
using Hex.Extensions;
using Hex.Grid.DetailQueue;
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
		
		private readonly List<UnitData> _hand = new();
		private readonly List<UnitData> _deck = new();
		private readonly List<UnitData> _discard = new();

		public bool IsHandEmpty => _hand.Count == 0;

		private void Awake()
		{
			deckPreviewQueue.DetailDequeued = OnDetailDequeued;
		}

		public void Initialize()
		{
			FillInitialDeck();
			FillHand();
			SetupPreviewQueue();
		}

		public void Dispose()
		{
			_deck.Clear();
			_hand.Clear();
			_discard.Clear();
			
			deckPreviewQueue.gameObject.SetActive(false);
			gameUI.PreviewQueueUI.gameObject.SetActive(false);
		}
		
		public void SetupPreviewQueue()
		{
			deckPreviewQueue.Initialize(GetUnitFromHand, _hand.Count);
			deckPreviewQueue.GeneratePreviewQueue();
			deckPreviewQueue.gameObject.SetActive(true);
            
			gameUI.PreviewQueueUI.Initialize(_hand.FirstOrDefault(), _hand.Count);
			gameUI.PreviewQueueUI.gameObject.SetActive(true);
		}
		
		private void FillInitialDeck()
		{
			foreach (var detail in startingDeck)
			{
				_deck.Add(detail);
			}
			_deck.Shuffle();
		}
		
		private void FillHand()
		{
			for (var i = 0; i < startingHandSize; i++)
			{
				if (_deck.Count == 0)
				{
					ShuffleDiscardIntoDeck();
					if (_deck.Count == 0) return;
				}
                
				_hand.Add(_deck[0]);
				_deck.RemoveAt(0);
			}
		}
		
		private void ShuffleDiscardIntoDeck()
		{
			_deck.AddRange(_discard);
			_deck.Shuffle();
			_discard.Clear();
		}
		
		private UnitData GetUnitFromHand(int index) => index < _hand.Count ? _hand[index] : null;

		private void OnDetailDequeued()
		{
			gameUI.PreviewQueueUI.SetNextAndDecrement(_hand.FirstOrDefault(), _hand.Count);
		}
		
		public void DrawNewHand()
		{
			_discard.AddRange(_hand);
			_hand.Clear();
			FillHand();
			SetupPreviewQueue();
		}

		public UnitData DrawNextUnit()
		{
			if (_hand.Count == 0)
			{
				Debug.LogError("Trying to draw on empty hand");
				return null;
			}
			
			// Draw next unit from hand then discard it
			var unit = _hand[0];
			_discard.Add(unit);
			_hand.RemoveAt(0);
			
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