using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The read-only branched entry that represents a single item in a collection and can be switched to another branch.
	/// </summary>
	public class BranchedItem<T>
	{
		internal BranchedEntry<T> Parent { get; set; }

		/// <summary>
		/// The item that this entry represents.
		/// </summary>
		public T Item { get; }

		/// <summary>
		/// The index of the branch in the item or the -1 if the item is not branched.
		/// </summary>
		/// <remarks>
		/// This property is read-only and can only be set during construction. <br/>
		/// Please use the <see cref="BranchedCollection{T}.CollectionChanged"/> event to capture changes to the conversation.
		/// </remarks>
		public int BranchIndex { get; }

		/// <summary>
		/// The number of branches available in the item.
		/// </summary>
		public int AvailableBranchesCount { get; }

		/// <summary>
		/// The event that is raised when the selected branch index needs to be changed.
		/// </summary>
		/// <remarks>
		/// Used in <see cref="BranchedCollection{T}"/> to update the collection.
		/// </remarks>
		public event EventHandler<int> BranchSelectionRequested;

		/// <summary>
		/// The event that is raised when a branch needs to be removed.
		/// </summary>
		/// <remarks>
		/// Used in <see cref="BranchedCollection{T}"/> to update the collection.
		/// </remarks>
		public event EventHandler<int> BranchRemoveRequested;

		/// <summary>
		/// Creates a new item entry with no branch information.
		/// </summary>
		/// <param name="item">The item.</param>
		public BranchedItem(T item)
		{
			Item = item;
			BranchIndex = -1;
			AvailableBranchesCount = 0;
		}

		/// <summary>
		/// Creates a new item entry with branch information.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="branchIndex">The current branch index for this item.</param>
		/// <param name="availableBranchesCount">The available branches count.</param>
		public BranchedItem(T item, int branchIndex, int availableBranchesCount)
		{
			Item = item;
			BranchIndex = branchIndex;
			AvailableBranchesCount = availableBranchesCount;
		}

		/// <summary>
		/// Requests a branch selection by index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public void SelectBranch(int branchIndex)
		{
			if (branchIndex < 0 || branchIndex >= AvailableBranchesCount)
				throw new ArgumentOutOfRangeException(nameof(branchIndex));

			BranchSelectionRequested?.Invoke(this, branchIndex);
		}

		/// <summary>
		/// Requests a branch removal by index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public void RemoveBranch(int branchIndex)
		{
			if (branchIndex < 0 || branchIndex >= AvailableBranchesCount)
				throw new ArgumentOutOfRangeException(nameof(branchIndex));

			BranchRemoveRequested?.Invoke(this, branchIndex);
		}

		/// <summary>
		/// Returns a copy of this item entry with the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>The copy of this instance but with specified item.</returns>
		public BranchedItem<T> With(T item)
		{
			return new BranchedItem<T>(item, BranchIndex, AvailableBranchesCount) { Parent = Parent };
		}
	}

	/// <summary>
	/// The branched entry that represents a branch with list of items and subsequent branches in a branched collection.
	/// </summary>
	internal class BranchedEntry<T>
	{
		public BranchedEntry<T> Parent { get; set; }

		/// <summary>
		/// The items in this branch before the next branches.
		/// </summary>
		public List<T> Items { get; }

		/// <summary>
		/// The branches that lead after items in <see cref="Items"/>.
		/// </summary>
		public List<BranchedEntry<T>> Branches { get; }

		/// <summary>
		/// The index of the selected branch or -1 if none is selected.
		/// </summary>
		public int SelectedBranchIndex { get; set; } = -1;

		public BranchedEntry()
		{
			Items = new List<T>();
			Branches = new List<BranchedEntry<T>>();
			SelectedBranchIndex = -1;
		}

		public BranchedEntry(List<T> items, List<BranchedEntry<T>> branches, int selectedBranchIndex)
		{
			Items = items ?? new List<T>();
			Branches = branches ?? new List<BranchedEntry<T>>();
			SelectedBranchIndex = selectedBranchIndex;

			if (Branches.Count > 0 && SelectedBranchIndex < 0)
				SelectedBranchIndex = 0;
			else if (Branches.Count == 0 && SelectedBranchIndex >= 0)
				SelectedBranchIndex = -1;
		}
	}
}