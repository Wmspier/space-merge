using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Extensions;
using UnityEngine;
using UnityEngine.Pool;

namespace Hex.Grid.DetailQueue
{
    public class DeckPreviewQueue : MonoBehaviour
    {
        [SerializeField] private Transform anchorFirst;
        [SerializeField] private Transform anchorSecond;
        [SerializeField] private Transform anchorThird;

        [SerializeField] private CardPreview previewPrefab;

        private readonly Queue<List<UnitData>> _queuedDequeueActions = new();
        private readonly Dictionary<int, CardPreview> _previews = new();
        
        private Func<int, UnitData> _getNext;

        public Action DetailDequeued;
        
        public bool ProcessingDequeues { get; private set; }

        public void Initialize(Func<int, UnitData> getNextFunc)
        {
            _getNext = getNextFunc;
        }

        public void GeneratePreviewQueue()
        {
            SetPreview(0, _getNext.Invoke(0));
            SetPreview(1, _getNext.Invoke(1));
            SetPreview(2, _getNext.Invoke(2));
        }

        private void SetPreview(int anchorIndex, UnitData unitData)
        {
            if (unitData == null)
            {
                return;
            }

            var anchor = anchorIndex switch
            {
                0 => anchorFirst,
                1 => anchorSecond,
                2 => anchorThird,
                _ => throw new IndexOutOfRangeException()
            };

            if (anchor.childCount > 0)
            {
                Destroy(anchor.GetChild(0).gameObject);
            }

            var newPreview = Instantiate(previewPrefab, anchor);
            newPreview.transform.Reset();
            newPreview.ApplyPreview(unitData, anchorIndex == 0);
            _previews[anchorIndex] = newPreview;
        }

        public void Dequeue(List<UnitData> queueSnapshot)
        {
            _queuedDequeueActions.Enqueue(queueSnapshot);

            if (!ProcessingDequeues)
            {
                ProcessDequeues();
            }
        }

        private async void ProcessDequeues()
        {
            ProcessingDequeues = true;
            while (_queuedDequeueActions.Count > 0)
            {
                await DequeueInternal(_queuedDequeueActions.Dequeue());
            }

            ProcessingDequeues = false;
        }

        private async Task DequeueInternal(List<UnitData> queueSnapshot)
        {
            DetailDequeued?.Invoke();
            if (!gameObject.activeSelf)
            {
                return;
            }

            var tasks = ListPool<Task>.Get();
            // Remove the first preview
            tasks.Add(_previews[0].ShrinkAndDestroy());
            
            // If there is a second preview lerp to first position
            if (_previews.TryGetValue(1, out var second))
            {
                tasks.Add(second.ApplyDetailAndLerp(queueSnapshot[0], anchorFirst, true, () =>
                {
                    _previews[0] = second;
                }));
            }

            // If there is a third position lerp to second position
            if (_previews.TryGetValue(2, out var third) && queueSnapshot[1] != null)
            {
                tasks.Add(third.ApplyDetailAndLerp(queueSnapshot[1], anchorSecond, false, () =>
                {
                    _previews[1] = third;
                    if (queueSnapshot[2] != null)
                    {
                        SetPreview(2, queueSnapshot[2]);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Clean up preview list after lerp is finished
            if (_previews.ContainsKey(1) && queueSnapshot[1] == null)
            {
                _previews.Remove(1);
            }
            if (_previews.ContainsKey(2) && queueSnapshot[2] == null)
            {
                _previews.Remove(2);
            }
            
            ListPool<UnitData>.Release(queueSnapshot);
        }
    }
}