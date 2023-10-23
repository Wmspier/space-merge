using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hex.Extensions;
using UnityEngine;
using UnityEngine.Pool;

namespace Hex.Grid.DetailQueue
{
    public class CellDetailQueue : MonoBehaviour
    {
        [SerializeField] private Transform anchorFirst;
        [SerializeField] private Transform anchorSecond;
        [SerializeField] private Transform anchorThird;

        [SerializeField] private CellDetailPreview previewPrefab;

        private readonly Queue<List<MergeCellDetailType>> _queuedDequeueActions = new();
        private readonly Dictionary<int, CellDetailPreview> _previews = new();
        
        private Func<int, MergeCellDetailType> _getNext;

        public Action DetailDequeued;
        
        public bool ProcessingDequeues { get; private set; }

        public void Initialize(Func<int, MergeCellDetailType> getNextFunc)
        {
            _getNext = getNextFunc;
        }

        public void GeneratePreviewQueue()
        {
            SetPreview(0, _getNext.Invoke(0));
            SetPreview(1, _getNext.Invoke(1));
            SetPreview(2, _getNext.Invoke(2));
        }

        private void SetPreview(int anchorIndex, MergeCellDetailType type)
        {
            if (type == MergeCellDetailType.Empty)
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
            newPreview.ApplyPreview(type, anchorIndex == 0);
            _previews[anchorIndex] = newPreview;
        }

        public void Dequeue(List<MergeCellDetailType> queueSnapshot)
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

        private async Task DequeueInternal(List<MergeCellDetailType> queueSnapshot)
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
            if (_previews.TryGetValue(2, out var third) && queueSnapshot[1] != MergeCellDetailType.Empty)
            {
                tasks.Add(third.ApplyDetailAndLerp(queueSnapshot[1], anchorSecond, false, () =>
                {
                    _previews[1] = third;
                    if (queueSnapshot[2] != MergeCellDetailType.Empty)
                    {
                        SetPreview(2, queueSnapshot[2]);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Clean up preview list after lerp is finished
            if (queueSnapshot[1] == MergeCellDetailType.Empty && _previews.ContainsKey(1))
            {
                _previews.Remove(1);
            }
            if (queueSnapshot[2] == MergeCellDetailType.Empty && _previews.ContainsKey(2))
            {
                _previews.Remove(2);
            }
            
            ListPool<MergeCellDetailType>.Release(queueSnapshot);
        }
    }
}