using System;
using System.Collections;
using System.Collections.Generic;

namespace RCLargeLanguageModels
{
	public class PriorityQueue<T, TP> : IEnumerable<T>
	{
		private class Entry
		{
			public T value;
			public TP priority;
		}

		private readonly IComparer<TP> comparer;
		private int count;
		private int capacity;
		private Entry[] items;

		public PriorityQueue() : this(Comparer<TP>.Default) { }

		public PriorityQueue(IComparer<TP> comparer)
		{
			this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			capacity = 11;
			items = new Entry[capacity];
		}

		public int Count => count;

		public T Dequeue()
		{
			if (count == 0)
				throw new InvalidOperationException("Queue is empty");

			T result = items[0].value;
			int newCount = --count;
			var lastItem = items[newCount];
			items[newCount] = default;

			if (newCount > 0)
				TrickleDown(0, lastItem);

			return result;
		}

		public T Peek()
		{
			if (count == 0)
				throw new InvalidOperationException("Queue is empty");
			return items[0].value;
		}

		public void Enqueue(T item, TP priority)
		{
			if (count == capacity)
				GrowHeap();
			count++;
			BubbleUp(count - 1, new Entry { value = item, priority = priority });
		}

		private void BubbleUp(int index, Entry entry)
		{
			while (index > 0)
			{
				int parentIndex = (index - 1) / 2;
				if (comparer.Compare(entry.priority, items[parentIndex].priority) < 0)
					break;

				items[index] = items[parentIndex];
				index = parentIndex;
			}
			items[index] = entry;
		}

		private void TrickleDown(int index, Entry entry)
		{
			int half = count / 2;
			while (index < half)
			{
				int childIndex = (index * 2) + 1;
				var child = items[childIndex];
				int rightChildIndex = childIndex + 1;
				if (rightChildIndex < count && comparer.Compare(child.priority, items[rightChildIndex].priority) <= 0)
				{
					childIndex = rightChildIndex;
					child = items[rightChildIndex];
				}
				if (comparer.Compare(entry.priority, child.priority) > 0)
				{
					break;
				}
				items[index] = child;
				index = childIndex;
			}
			items[index] = entry;
		}

		private void GrowHeap()
		{
			int oldCapacity = capacity;
			capacity = oldCapacity * 2;
			Array.Resize(ref items, capacity);
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < count; i++)
			{
				yield return items[i].value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}