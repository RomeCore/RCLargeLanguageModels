using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using RCLargeLanguageModels.Utilities;

namespace RCLargeLanguageModels
{
	// Жесть нейронки тупые конечно, приходится все делать самому
	// Потрачено 3-4 дня

	/// <summary>
	/// Represents an edit mode for editing a item in a branched collection.
	/// </summary>
	public enum BranchItemEditMode
	{
		/// <summary>
		/// Simply replaces the item without branching.
		/// </summary>
		Default,

		/// <summary>
		/// Creates a new branch with only the edited item.
		/// </summary>
		NewBranch,

		/// <summary>
		/// Creates a new branch with the edited item and copies subsequent items and branches.
		/// </summary>
		NewBranchWithCopy
	}

	/// <summary>
	/// Represents a mode for removing a branch from a branched collection.
	/// </summary>
	public enum BranchRemoveMode
	{
		/// <summary>
		/// Simply removes the branch, keeps the sibling branch if it single.
		/// </summary>
		Default,

		/// <summary>
		/// Removes the branch and can merge the sibling branch if it single.
		/// </summary>
		Merge
	}

	/// <summary>
	/// Represents a branched collection of items.
	/// </summary>
	public class BranchedCollection<T> : IEnumerable<BranchedItem<T>>, INotifyCollectionChanged
	{
		/// <summary>
		/// The synchronization lock for the collection.
		/// </summary>
		private readonly object _syncLock = new object();

		/// <summary>
		/// The collection of currently stored item entries
		/// </summary>
		private readonly List<BranchedItem<T>> _items;

		/// <summary>
		/// The root element of the tree.
		/// </summary>
		private readonly BranchedEntry<T> _root;

		/// <summary>
		/// The last selected item entry in the collection, used for adding items to the end.
		/// </summary>
		private BranchedEntry<T> _last;

		/// <summary>
		/// Gets the root element of the item tree.
		/// </summary>
		internal BranchedEntry<T> Root => _root;

		/// <summary>
		/// Gets the current collection of items.
		/// </summary>
		public IReadOnlyList<BranchedItem<T>> Items { get; }

		private bool _locked = false;
		/// <summary>
		/// Gets a value indicating whether the history is locked.
		/// </summary>
		/// <remarks>
		/// When the history is locked, all changes to the history are ignored.
		/// </remarks>
		public bool Locked => _locked;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual void CollectionChangedHandler(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);

			if (e.OldItems != null)
			{
				foreach (BranchedItem<T> oldItem in e.OldItems)
				{
					oldItem.BranchSelectionRequested -= OnBranchSelectionRequested;
					oldItem.BranchRemoveRequested -= OnBranchRemoveRequested;
				}
			}
			if (e.NewItems != null)
			{
				foreach (BranchedItem<T> newItem in e.NewItems)
				{
					newItem.BranchSelectionRequested += OnBranchSelectionRequested;
					newItem.BranchRemoveRequested += OnBranchRemoveRequested;
				}
			}
		}

		private void OnBranchSelectionRequested(object sender, int e)
		{
			var entry = (BranchedItem<T>)sender;
			SelectBranch(entry, e);
		}

		private void OnBranchRemoveRequested(object sender, int e)
		{
			var entry = (BranchedItem<T>)sender;
			RemoveBranch(entry, e);
		}

		private void CollectionReplace(int index, BranchedItem<T> entry)
		{
			var oldEntry = _items[index];
			_items[index] = entry;
			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, entry, oldEntry, index));
		}

		/// <summary>
		/// Replaces items that leads after the specified index with the specified items.
		/// </summary>
		private void CollectionReplaceRange(int startIndex, List<BranchedItem<T>> items)
		{
			CollectionReplaceRange(startIndex, _items.Count - startIndex, items);
		}
		
		/// <summary>
		/// Replaces items that leads after the specified index with the specified items.
		/// </summary>
		private void CollectionReplaceRange(int startIndex, int count, List<BranchedItem<T>> items)
		{
			if (startIndex == _items.Count)
				return;

			var oldItems = _items.Skip(startIndex).Take(count).ToList();

			_items.RemoveRange(startIndex, count);
			_items.AddRange(items);

			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
				items, oldItems, startIndex));
		}

		/// <summary>
		/// Removes items that leads after the specified index.
		/// </summary>
		private void CollectionRemove(int index)
		{
			var oldItem = _items[index];

			_items.RemoveAt(index);

			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
				oldItem, index));
		}
		
		/// <summary>
		/// Removes items that leads after the specified index.
		/// </summary>
		private void CollectionRemoveRange(int startIndex)
		{
			if (startIndex == _items.Count)
				return;

			var olditems = _items.Skip(startIndex).ToList();

			_items.RemoveRange(startIndex, _items.Count - startIndex);

			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
				olditems, startIndex));
		}

		private void CollectionClear()
		{
			var previtems = _items.ToList();
			_items.Clear();
			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, previtems));
		}

		private void CollectionAdd(BranchedItem<T> item)
		{
			int index = _items.Count;
			_items.Add(item);
			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		private void CollectionAdd(List<BranchedItem<T>> items)
		{
			int index = _items.Count;
			_items.AddRange(items);
			CollectionChangedHandler(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index));
		}

		/// <summary>
		/// Creates a shallow recursive copy of the branch (does not copies items, since they are immutable)
		/// </summary>
		private BranchedEntry<T> BranchCopy(BranchedEntry<T> entry)
		{
			var copied = new BranchedEntry<T>();

			copied.Items.AddRange(entry.Items);
			copied.Branches.AddRange(entry.Branches.Select(e =>
			{
				var copiedInnerBranch = BranchCopy(e);
				copiedInnerBranch.Parent = copied;
				return copiedInnerBranch;
			}));

			return copied;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the current item entries in the history.
		/// </summary>
		/// <returns>An enumerator for the current item entries.</returns>
		public IEnumerator<BranchedItem<T>> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/*IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			foreach (var entry in _items)
				yield return entry.Item;
		}*/

		/// <summary>
		/// Initializes a new instance of the <see cref="BranchedCollection{T}"/> class with an empty item tree.
		/// </summary>
		public BranchedCollection() : this(new BranchedEntry<T>())
		{
		}

		internal BranchedCollection(BranchedEntry<T> root)
		{
			_items = new List<BranchedItem<T>>();
			Items = _items.AsReadOnly();
			_root = root;
			_root.Parent = null;
			RebuildParents(_root);

			var (items, last) = BuildEntries(_root);
			_last = last;
			CollectionAdd(items);
		}

		private static void RebuildParents(BranchedEntry<T> root)
		{
			foreach (var branch in root.Branches)
			{
				branch.Parent = root;
				RebuildParents(branch);
			}
		}

		/// <summary>
		/// Builds a list of item entries from the item tree and returns the last entry.
		/// </summary>
		private static (List<BranchedItem<T>> Entries, BranchedEntry<T> Last) BuildEntries(BranchedEntry<T> root)
		{
			var entries = new List<BranchedItem<T>>();
			BranchedEntry<T> last = root;

			while (true)
			{
				int index = 0;
				foreach (var item in last.Items)
				{
					if (last.Parent != null && index == 0)
					{
						entries.Add(new BranchedItem<T>(item, last.Parent.SelectedBranchIndex, last.Parent.Branches.Count) { Parent = last });
						index++;
					}
					else
					{
						entries.Add(new BranchedItem<T>(item) { Parent = last });
					}
				}

				if (last.Branches.Count == 0)
				{
					last.SelectedBranchIndex = -1;
					break;
				}

				// Force the first branch to be selected if the index is out of range.
				if (last.SelectedBranchIndex < 0 || last.SelectedBranchIndex >= last.Branches.Count)
					last.SelectedBranchIndex = 0;

				last = last.Branches[last.SelectedBranchIndex];
			}

			return (entries, last);
		}

		/// <summary>
		/// Locks the item history, preventing any further changes.
		/// </summary>
		/// <remarks>
		/// When locked, all modification methods (e.g., <see cref="Add(T)"/>, <see cref="Edit(int, T, BranchedItem{T})"/>) are ignored.
		/// This is useful to prevent changes while receiving a new item from an assistant, for example.
		/// </remarks>
		public void Lock()
		{
			lock (_syncLock)
				_locked = true;
		}

		/// <summary>
		/// Unlocks the item history, allowing further changes.
		/// </summary>
		public void Unlock()
		{
			lock (_syncLock)
				_locked = false;
		}

		/// <summary>
		/// Adds a single item to the end of the current branch in the history.
		/// </summary>
		/// <param name="item">The item to add. Must not be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		public virtual void Add(T item)
		{
			if (_locked)
				return;
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_syncLock)
			{
				int branchIndex = -1, availableBranchesCount = 0;

				if (_last.Items.Count == 0 && _last.Parent != null)
				{
					branchIndex = _last.Parent.SelectedBranchIndex;
					availableBranchesCount = _last.Parent.Branches.Count;
				}

				_last.Items.Add(item);
				CollectionAdd(new BranchedItem<T>(item, branchIndex, availableBranchesCount) { Parent = _last });
			}
		}

		/// <summary>
		/// Adds a collection of items to the end of the current branch in the history.
		/// </summary>
		/// <param name="items">The items to add. Must not be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
		public virtual void AddRange(IEnumerable<T> items)
		{
			if (_locked)
				return;
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			lock (_syncLock)
			{
				int branchIndex = -1, availableBranchesCount = 0;

				if (_last.Items.Count == 0 && _last.Parent != null)
				{
					branchIndex = _last.Parent.SelectedBranchIndex;
					availableBranchesCount = _last.Parent.Branches.Count;
				}

				_last.Items.AddRange(items);
				CollectionAdd(items.Select(m =>
				{
					var entry = new BranchedItem<T>(m, branchIndex, availableBranchesCount) { Parent = _last };

					branchIndex = -1;
					availableBranchesCount = 0;

					return entry;
				}).ToList());
			}
		}

		/// <summary>
		/// Adds a collection of items to the end of the current branch in the collection.
		/// </summary>
		/// <param name="items">The items to add. Must not be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
		public void AddRange(params T[] items)
		{
			AddRange(items as IEnumerable<T>);
		}

		public void Replace(T sourceItem, T item)
		{
			lock (_syncLock)
			{
				var index = _items.FindIndex(e => Equals(e.Item, sourceItem));
				if (index != -1)
				{
					Edit(index, item, BranchItemEditMode.Default);
					return;
				}

				var findResult = FindInTree(Root, sourceItem);
				if (findResult.LocalIndex != -1)
				{
					findResult.Parent.Items[findResult.LocalIndex] = item;
					return;
				}

				throw new ArgumentException("The item is not found in branched collection!", nameof(sourceItem));
			}
		}

		/// <summary>
		/// Removes the item at the specified index from the collection.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than or equal to the collection count.</exception>
		public void RemoveAt(int index)
		{
			lock (_syncLock)
			{
				if (index < 0 || index >= _items.Count)
					throw new ArgumentOutOfRangeException(nameof(index));

				var entry = _items[index];
				var parent = entry.Parent;
				int localIndex = parent.Items.IndexOf(entry.Item);

				if (parent.Items.Count == 1)
				{
					// The item is the only one in the parent.
					// Combine the branches with the parent and notify collection replacement.

					var grandparent = parent.Parent;

					if (grandparent != null)
					{
						var branchIndex = parent.SelectedBranchIndex;
						var grandparentBranchIndex = grandparent.SelectedBranchIndex;
						grandparent.Branches.RemoveAt(grandparentBranchIndex);
						grandparent.Branches.InsertRange(grandparentBranchIndex, parent.Branches);
						grandparent.SelectedBranchIndex = grandparentBranchIndex + branchIndex;
						CollectionReplaceRange(index, 2, new List<BranchedItem<T>> { new BranchedItem<T>(entry.Item,
						grandparent.SelectedBranchIndex, grandparent.Branches.Count) { Parent = grandparent } });
					}
					else
					{
						parent.Items.Clear();
						CollectionRemove(0);
					}
				}
				else if (localIndex == 0)
				{
					// The item is the first one in the parent.

					var grandparent = parent.Parent;
					parent.Items.RemoveAt(0);
					var next = parent.Items[0];
					CollectionReplaceRange(index, 2, new List<BranchedItem<T>> { new BranchedItem<T>(next,
					grandparent?.SelectedBranchIndex ?? -1, grandparent?.Branches.Count ?? 0) });
				}
				else
				{
					parent.Items.RemoveAt(localIndex);
					CollectionRemove(localIndex);
				}
			}
		}

		/// <summary>
		/// Removes the first occurrence of the specified item from the collection.
		/// </summary>
		/// <param name="item">The item to remove from the collection.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when the item is not found in the current branch.</exception>
		public void Remove(T item)
		{
			RemoveAt(FindIndex(item));
		}

		/// <summary>
		/// Removes the first occurrence of the specified branched item from the collection.
		/// </summary>
		/// <param name="item">The branched item to remove from the collection.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when the item is not found in the current branch.</exception>
		public void Remove(BranchedItem<T> item)
		{
			RemoveAt(FindIndex(item));
		}

		/// <summary>
		/// Clears all items and branches from the collection.
		/// </summary>
		public virtual void Clear()
		{
			if (_locked)
				return;

			lock (_syncLock)
			{
				_last = _root;
				_last.Items.Clear();
				_last.Branches.Clear();
				CollectionClear();
			}
		}

		private int FindIndex(T sourceItem)
		{
			if (sourceItem == null)
				throw new ArgumentNullException(nameof(sourceItem));

			int index;

			lock (_syncLock)
			{
				index = _items.FindIndex(e => Equals(e.Item, sourceItem));
				if (index < 0)
					throw new ArgumentException("The item is not found in the current history branch.", nameof(sourceItem));
			}

			return index;
		}

		private int FindIndex(BranchedItem<T> sourceEntry)
		{
			if (sourceEntry == null)
				throw new ArgumentNullException(nameof(sourceEntry));

			int index;

			lock (_syncLock)
			{
				index = _items.IndexOf(sourceEntry);
				if (index < 0)
					throw new ArgumentException("The item entry is not found in the current history branch.", nameof(sourceEntry));
			}

			return index;
		}

		private (BranchedEntry<T> Parent, int LocalIndex) FindInTree(BranchedEntry<T> root, T item)
		{
			int index = root.Items.IndexOf(item);
			if (index != -1)
				return (root, index);

			if (root.Branches.Count > 0)
			{
				// Prioritize the selected branch
				var result = FindInTree(root.Branches[root.SelectedBranchIndex], item);
				if (result.LocalIndex != -1)
					return result;

				int _i = 0;
				foreach (var branch in root.Branches)
				{
					if (_i++ == root.SelectedBranchIndex)
						continue;

					result = FindInTree(branch, item);
					if (result.LocalIndex != -1)
						return result;
				}
			}

			return (root, -1);
		}

		#region Editing

		/// <summary>
		/// Places the new branch at the specified index in the parent branch.
		/// </summary>
		private void PlaceNewBranch(ref BranchedEntry<T> parent, int localIndex, BranchedEntry<T> newBranch)
		{
			if (localIndex > 0)
			{
				// Split the parent branch into two branches

				var secondSplitBranch = new BranchedEntry<T> { Parent = parent };
				secondSplitBranch.Branches.AddRange(parent.Branches);
				secondSplitBranch.Items.AddRange(parent.Items.Skip(localIndex));

				parent.Branches.Clear();
				parent.Branches.Add(secondSplitBranch);
				parent.Items.RemoveRange(localIndex, parent.Items.Count - localIndex);
			}
			else
			{
				// This is the first item in the branch, so we don't need to split the branch

				var parent2 = parent.Parent;

				if (parent2 == null)
				{
					// Its the first item in the history (branch is root)

					// We need to copy the root branch and clear the root branch, then we need to add the copy into root
					var copiedRoot = new BranchedEntry<T> { Parent = parent };
					copiedRoot.Items.AddRange(parent.Items);
					copiedRoot.Branches.AddRange(parent.Branches);
					copiedRoot.SelectedBranchIndex = parent.SelectedBranchIndex;

					parent.Items.Clear();
					parent.Branches.Clear();
					parent.Branches.Add(copiedRoot);
					
					if (parent.Branches.Count == 0)
						parent.SelectedBranchIndex = -1;
					else
						parent.SelectedBranchIndex = Utils.Clamp(parent.SelectedBranchIndex, 0, parent.Branches.Count - 1);
				}
				else
				{
					parent = parent2;
				}
			}

			// Add the new branch to the parent and select it
			newBranch.Parent = parent;
			parent.Branches.Add(newBranch);
			parent.SelectedBranchIndex = parent.Branches.Count - 1;
		}

		/// <summary>
		/// Just the edits the item without changing the branch structure.
		/// </summary>
		private void Edit(int index, T item, BranchedItem<T> entry)
		{
			var parent = entry.Parent;
			int localIndex = parent.Items.IndexOf(entry.Item);

			// Replace the old item with the new item
			parent.Items[localIndex] = item;
			CollectionReplace(index, entry.With(item));
		}

		/// <summary>
		/// Places the branch at the specified index and adds the item at the new branch.
		/// </summary>
		private void EditNewBranch(int index, T item, BranchedItem<T> entry)
		{
			var parent = entry.Parent;
			int localIndex = parent.Items.IndexOf(entry.Item);

			// Add new branch with the edited item
			var newBranch = new BranchedEntry<T>();
			newBranch.Items.Add(item);
			PlaceNewBranch(ref parent, localIndex, newBranch);

			var newEntry = new BranchedItem<T>(item, parent.Branches.Count - 1, parent.Branches.Count) { Parent = newBranch };
			_last = newBranch;
			CollectionReplaceRange(index, new List<BranchedItem<T>> { newEntry });
		}

		/// <summary>
		/// Places the copy of the branch at the specified index and edits the first item in the new branch.
		/// </summary>
		private void EditCopyBranch(int index, T item, BranchedItem<T> entry)
		{
			var parent = entry.Parent;
			int localIndex = parent.Items.IndexOf(entry.Item);
			
			var newBranch = BranchCopy(parent);

			// Since the new branch is a copy of the original branch, we need to remove the first item from it
			if (localIndex > 0)
				newBranch.Items.RemoveRange(0, localIndex);

			// Edit the first item in the new branch
			if (newBranch.Items.Count > 0)
				newBranch.Items[0] = item;
			else
				newBranch.Items.Add(item);

			PlaceNewBranch(ref parent, localIndex, newBranch);

			var (newEntries, last) = BuildEntries(newBranch);
			_last = last;
			CollectionReplaceRange(index, newEntries);
		}

		/// <summary>
		/// Places a new branch at the specified index in the current branch.
		/// </summary>
		/// <param name="sourceIndex">The index of the item to place new branch.</param>
		public void PlaceEmptyBranch(int sourceIndex)
		{
			if (_locked)
				return;

			lock (_syncLock)
			{
				if (sourceIndex < 0 || sourceIndex >= _items.Count)
					throw new ArgumentOutOfRangeException(nameof(sourceIndex));

				var entry = _items[sourceIndex];
				var parent = entry.Parent;
				int localIndex = parent.Items.IndexOf(entry.Item);

				var newBranch = _last = new BranchedEntry<T>();
				PlaceNewBranch(ref parent, localIndex, newBranch);
				CollectionRemoveRange(sourceIndex);
			}
		}

		/// <summary>
		/// Places a new branch at the specified item in the current branch.
		/// </summary>
		/// <param name="sourceItem">The item where to place new branch.</param>
		public void PlaceEmptyBranch(T sourceItem)
		{
			PlaceEmptyBranch(FindIndex(sourceItem));
		}

		/// <summary>
		/// Places a new branch at the specified entry in the current branch.
		/// </summary>
		/// <param name="sourceItem">The item entry there to place new branch.</param>
		public void PlaceEmptyBranch(BranchedItem<T> sourceItem)
		{
			PlaceEmptyBranch(FindIndex(sourceItem));
		}

		/// <summary>
		/// Edits a item at the specified index in the current branch.
		/// </summary>
		/// <param name="sourceIndex">The index of the item to edit.</param>
		/// <param name="editedItem">The new item content. Must not be <see langword="null"/>.</param>
		/// <param name="mode">The edit mode to apply. Defaults to <see cref="BranchItemEditMode.Default"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="editedItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sourceIndex"/> is less than 0 or greater than or equal to the item count.</exception>
		public virtual void Edit(int sourceIndex, T editedItem, BranchItemEditMode mode = BranchItemEditMode.Default)
		{
			if (_locked)
				return;
			if (editedItem == null)
				throw new ArgumentNullException(nameof(editedItem));

			lock (_syncLock)
			{
				if (sourceIndex < 0 || sourceIndex >= _items.Count)
					throw new ArgumentOutOfRangeException(nameof(sourceIndex));

				var entry = _items[sourceIndex];

				switch (mode)
				{
					case BranchItemEditMode.Default:
						Edit(sourceIndex, editedItem, entry);
						break;

					case BranchItemEditMode.NewBranch:
						EditNewBranch(sourceIndex, editedItem, entry);
						break;

					case BranchItemEditMode.NewBranchWithCopy:
						EditCopyBranch(sourceIndex, editedItem, entry);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(mode));
				}
			}
		}

		/// <summary>
		/// Edits a item identified by its content in the current branch.
		/// </summary>
		/// <param name="sourceItem">The original item to edit. Must not be <see langword="null"/>.</param>
		/// <param name="editedItem">The new item content. Must not be <see langword="null"/>.</param>
		/// <param name="mode">The edit mode to apply. Defaults to <see cref="BranchItemEditMode.Default"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceItem"/> or <paramref name="editedItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceItem"/> is not found in the current branch.</exception>
		public void Edit(T sourceItem, T editedItem, BranchItemEditMode mode = BranchItemEditMode.Default)
		{
			Edit(FindIndex(sourceItem), editedItem, mode);
		}

		/// <summary>
		/// Edits a item identified by its entry in the current branch.
		/// </summary>
		/// <param name="sourceItem">The original item entry to edit. Must not be <see langword="null"/>.</param>
		/// <param name="editedItem">The new item content. Must not be <see langword="null"/>.</param>
		/// <param name="mode">The edit mode to apply. Defaults to <see cref="BranchItemEditMode.Default"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceItem"/> or <paramref name="editedItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceItem"/> is not found in the current branch.</exception>
		public void Edit(BranchedItem<T> sourceItem, T editedItem, BranchItemEditMode mode = BranchItemEditMode.Default)
		{
			Edit(FindIndex(sourceItem), editedItem, mode);
		}

		#endregion

		/// <summary>
		/// Selects a branch at the specified item index in the current history.
		/// </summary>
		/// <param name="sourceIndex">The index of the item whose branch to select.</param>
		/// <param name="branchIndex">The index of the branch to select.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sourceIndex"/> or <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the item at <paramref name="sourceIndex"/> has no branches.</exception>
		public virtual void SelectBranch(int sourceIndex, int branchIndex)
		{
			if (_locked)
				return;

			lock (_syncLock)
			{
				if (sourceIndex < 0 || sourceIndex >= _items.Count)
					throw new ArgumentOutOfRangeException(nameof(sourceIndex));

				var entry = _items[sourceIndex];
				if (entry.AvailableBranchesCount == 0)
					throw new InvalidOperationException("The target item has no branches.");
				if (entry.BranchIndex == branchIndex)
					return;
				if (branchIndex < 0 || branchIndex >= entry.AvailableBranchesCount)
					throw new ArgumentOutOfRangeException(nameof(branchIndex));

				var parent = entry.Parent; // The parent tree element of the entry; the entry has index 0 in its `items` list
				var parent2 = parent.Parent; // The grandparent tree element of the entry; it contains our `parent` tree element

				parent2.SelectedBranchIndex = branchIndex;
				var branch = parent2.Branches[branchIndex];

				var (newEntries, last) = BuildEntries(branch);
				_last = last;
				CollectionReplaceRange(sourceIndex, newEntries);
			}
		}

		/// <summary>
		/// Selects a branch for a specified item in the current history.
		/// </summary>
		/// <param name="sourceItem">The item whose branch to select. Must not be <see langword="null"/>.</param>
		/// <param name="branchIndex">The index of the branch to select.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceItem"/> is not found in the current branch.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the item has no branches.</exception>
		public void SelectBranch(T sourceItem, int branchIndex)
		{
			SelectBranch(FindIndex(sourceItem), branchIndex);
		}

		/// <summary>
		/// Selects a branch for a specified item entry in the current history.
		/// </summary>
		/// <param name="sourceItem">The item entry whose branch to select. Must not be <see langword="null"/>.</param>
		/// <param name="branchIndex">The index of the branch to select.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceItem"/> is not found in the current branch.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the entry has no branches.</exception>
		public void SelectBranch(BranchedItem<T> sourceItem, int branchIndex)
		{
			SelectBranch(FindIndex(sourceItem), branchIndex);
		}

		/// <summary>
		/// Removes a branch at the specified item index in the current collection and updates the item collection accordingly.
		/// </summary>
		/// <param name="sourceIndex">The index of the item whose branch to remove.</param>
		/// <param name="branchIndex">The index of the branch to remove.</param>
		/// <param name="mode">The remove mode to apply. Defaults to <see cref="BranchRemoveMode.Default"/>.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sourceIndex"/> or <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the item at <paramref name="sourceIndex"/> has no branches.</exception>
		public virtual void RemoveBranch(int sourceIndex, int branchIndex, BranchRemoveMode mode = BranchRemoveMode.Default)
		{
			if (_locked)
				return;

			switch (mode)
			{
				case BranchRemoveMode.Default:
				case BranchRemoveMode.Merge:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode));
			}

			lock (_syncLock)
			{
				if (sourceIndex < 0 || sourceIndex >= _items.Count)
					throw new ArgumentOutOfRangeException(nameof(sourceIndex));

				var entry = _items[sourceIndex];
				if (entry.AvailableBranchesCount == 0)
					throw new InvalidOperationException("The target item has no branches.");
				if (branchIndex < 0 || branchIndex >= entry.AvailableBranchesCount)
					throw new ArgumentOutOfRangeException(nameof(branchIndex));

				var parent = entry.Parent; // The parent tree element of the entry; the entry has index 0 in its `items` list
				var parent2 = parent.Parent; // The grandparent tree element of the entry; it contains our `parent` tree element
				var prevBranch = parent2.Branches[parent2.SelectedBranchIndex];

				if (entry.AvailableBranchesCount == 1)
				{
					// The branch is the only one, so we can just remove this branch

					parent2.Branches.Clear();
					parent2.SelectedBranchIndex = -1;
					_last = parent2;
					CollectionRemoveRange(sourceIndex);

					return;
				}

				int newIndex = branchIndex < entry.BranchIndex ? entry.BranchIndex : Math.Max(entry.BranchIndex - 1, 0);
				parent2.Branches.RemoveAt(branchIndex);
				parent2.SelectedBranchIndex = newIndex;
				var branch = parent2.Branches[newIndex]; // The newly selected branch

				if (mode == BranchRemoveMode.Merge && parent2.Branches.Count == 1)
				{
					// Merge the branch into the parent branch

					branch.Parent = null;
					parent2.Items.AddRange(branch.Items);
					parent2.Branches.Clear();
					parent2.Branches.AddRange(branch.Branches);
				}

				if (prevBranch != branch)
				{
					// The branch has changed (when selected branch is removed)

					var (newEntries, last) = BuildEntries(branch);
					_last = last;
					CollectionReplaceRange(sourceIndex, newEntries);
				}
				else
				{
					if (mode == BranchRemoveMode.Merge)
					{
						if (branch.Items.Count == parent2.Items.Count)
							CollectionReplace(sourceIndex, new BranchedItem<T>(entry.Item, newIndex, parent2.Branches.Count) { Parent = parent2 });
						else
							CollectionReplace(sourceIndex, new BranchedItem<T>(entry.Item) { Parent = parent2 });
					}
					else
						CollectionReplace(sourceIndex, new BranchedItem<T>(entry.Item, newIndex, branch.Branches.Count) { Parent = branch });
				}
			}
		}

		/// <summary>
		/// Removes a branch for a specified item in the current tree.
		/// </summary>
		/// <param name="sourceItem">The item whose branch to remove. Must not be <see langword="null"/>.</param>
		/// <param name="branchIndex">The index of the branch to remove.</param>
		/// <param name="mode">The remove mode to apply. Defaults to <see cref="BranchRemoveMode.Default"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceItem"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceItem"/> is not found in the current branch.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the item has no branches.</exception>
		public void RemoveBranch(T sourceItem, int branchIndex, BranchRemoveMode mode = BranchRemoveMode.Default)
		{
			RemoveBranch(FindIndex(sourceItem), branchIndex, mode);
		}

		/// <summary>
		/// Removes a branch for a specified item entry in the current history.
		/// </summary>
		/// <param name="sourceEntry">The item entry whose branch to remove. Must not be <see langword="null"/>.</param>
		/// <param name="branchIndex">The index of the branch to remove.</param>
		/// <param name="mode">The remove mode to apply. Defaults to <see cref="BranchRemoveMode.Default"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceEntry"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="sourceEntry"/> is not found in the current branch.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="branchIndex"/> is out of range.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the entry has no branches.</exception>
		public void RemoveBranch(BranchedItem<T> sourceEntry, int branchIndex, BranchRemoveMode mode = BranchRemoveMode.Default)
		{
			RemoveBranch(FindIndex(sourceEntry), branchIndex, mode);
		}
	}
}