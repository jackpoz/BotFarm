using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    [Flags]
    public enum ActionFlag
    {
        None = 0x0,
        Movement = 0x1
    }

    public class RepeatingAction
    {
        public Action Action
        {
            get;
            private set;
        }

        public Action Cancel
        {
            get;
            private set;
        }

        public DateTime ScheduledTime
        {
            get;
            private set;
        }

        public TimeSpan Interval
        {
            get;
            private set;
        }

        public ActionFlag Flags
        {
            get;
            private set;
        }

        public int Id
        {
            get;
            private set;
        }

        public RepeatingAction(Action action, Action cancel, DateTime scheduledTime, TimeSpan interval, ActionFlag flags, int id)
        {
            this.Action = action;
            this.Cancel = cancel ?? new Action(() => { });
            this.ScheduledTime = scheduledTime;
            this.Interval = interval;
            this.Flags = flags;
            this.Id = id;
        }
    }

    public class ScheduledActions : IList<RepeatingAction>
    {
        List<RepeatingAction> actions;

        public ScheduledActions()
        {
            actions = new List<RepeatingAction>();
        }

        public int IndexOf(RepeatingAction item)
        {
            return actions.IndexOf(item);
        }

        void IList<RepeatingAction>.Insert(int index, RepeatingAction item)
        {
            Add(item);
        }

        public void RemoveAt(int index)
        {
            RemoveAt(index, true);
        }

        public void RemoveAt(int index, bool cancel)
        {
            var action = actions[index];
            actions.RemoveAt(index);
            if (cancel)
                action.Cancel();
        }

        public RepeatingAction this[int index]
        {
            get
            {
                return actions[index];
            }
            set
            {
                actions[index] = value;
                Sort();
            }
        }

        public void Add(RepeatingAction item)
        {
            actions.Add(item);
            Sort();
        }

        public void Clear()
        {
            Clear(true);
        }

        public void Clear(bool cancel)
        {
            if (cancel)
            {
                while (actions.Count > 0)
                    RemoveAt(0, true);
            }
            else
                actions.Clear();
        }

        public bool Contains(RepeatingAction item)
        {
            return actions.Contains(item);
        }

        public void CopyTo(RepeatingAction[] array, int arrayIndex)
        {
            actions.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return actions.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(RepeatingAction item)
        {
            return Remove(item, true);
        }

        public bool Remove(RepeatingAction item, bool cancel)
        {
            var removed = actions.Remove(item);
            if (cancel)
                item.Cancel();
            return removed;
        }

        public int RemoveByFlag(ActionFlag flag, bool cancel = true)
        {
            var actionsWithFlag = actions.Where(action => action.Flags.HasFlag(flag)).ToList();
            actions.RemoveAll(action => action.Flags.HasFlag(flag));
            if (cancel)
                actionsWithFlag.ForEach(action => action.Cancel());
            return actionsWithFlag.Count;
        }

        public bool Remove(int actionId, bool cancel = true)
        {
            var actionsWithId = actions.Where(action => action.Id == actionId).ToList();
            actions.RemoveAll(action => action.Id == actionId);
            if (cancel)
                actionsWithId.ForEach(action => action.Cancel());
            return actionsWithId.Count > 0;
        }

        public IEnumerator<RepeatingAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        void Sort()
        {
            actions.Sort((a, b) => (int)(a.ScheduledTime - b.ScheduledTime).TotalMilliseconds);
        }
    }
}
