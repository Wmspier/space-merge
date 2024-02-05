using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Extensions;
using UnityEngine;
using UnityEngine.Pool;

namespace Hex.Grid.DetailQueue
{
    public class DeckPreviewQueue : MonoBehaviour
    {
        [SerializeField] private float previewOffsetY = 1f;
        [SerializeField] private CardPreview previewPrefab;

        private readonly Queue<CardPreview> _previewQueue = new();
        private readonly Queue<List<UnitData>> _dataSnapshotQueue = new();
        
        private Func<int, UnitData> _getNext;
        private int _dequeueCount;
        private int previewQueueSize;

        public Action DetailDequeued;
        
        public bool ProcessingDequeues { get; private set; }

        public void Initialize(Func<int, UnitData> getNextFunc, int queueSize)
        {
            _getNext = getNextFunc;
            previewQueueSize = queueSize;
            
            transform.DestroyAllChildGameObjects();
            _previewQueue.Clear();
            _dataSnapshotQueue.Clear();
        }

        public void GeneratePreviewQueue()
        {
            for (var i = 0; i < previewQueueSize; i++)
            {
                var unit = _getNext.Invoke(i);
                if (unit == null) return;
                SetPreview(i, unit);
            }
        }

        private Task SetPreview(int previewIndex, UnitData unitData)
        {
            var previewPosition = GetPositionFromIndex(previewIndex);

            var newPreview = Instantiate(previewPrefab, transform);
            newPreview.transform.Reset();
            newPreview.ApplyPreview(unitData, previewIndex == 0);
            newPreview.transform.position = previewPosition;
            _previewQueue.Enqueue(newPreview);

            return Task.CompletedTask;
        }
        
        public void Dequeue(List<UnitData> queueSnapshot)
        {
            _dequeueCount++;
            _dataSnapshotQueue.Enqueue(queueSnapshot);

            if (!ProcessingDequeues)
            {
                ProcessDequeues();
            }
        }

        private async void ProcessDequeues()
        {
            ProcessingDequeues = true;
            while (_dequeueCount > 0)
            {
                DetailDequeued?.Invoke();
                _dequeueCount--;

                await DequeueInternal();
            }

            ProcessingDequeues = false;
        }

        private async Task DequeueInternal()
        {
            var dataSnapshot = _dataSnapshotQueue.Dequeue();
            var tasks = ListPool<Task>.Get();
            
            // Dequeue and destroy next in queue
            tasks.Add(_previewQueue.Dequeue().ShrinkAndDestroy());
            
            // Lerp queue
            for (var i = 0; i < _previewQueue.Count; i++)
            {
                var nextInQueue = _previewQueue.ElementAt(i);
                tasks.Add(nextInQueue.ApplyDetailAndLerp(dataSnapshot[i], GetPositionFromIndex(i), i==0, i));
            }

            // Enqueue and spawn next in data snapshot
            if (dataSnapshot.Count >= previewQueueSize)
            {
                tasks.Add(SetPreview(previewQueueSize, dataSnapshot[previewQueueSize-1]));
            }
            
            await Task.WhenAll(tasks);
        }
        
        private Vector3 GetPositionFromIndex(int index)
        {
            var previewPosition = transform.position;
            previewPosition.y -= (index + 1) * previewOffsetY;
            return previewPosition;
        }
    }
}