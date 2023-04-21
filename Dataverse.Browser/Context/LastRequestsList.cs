using System;
using System.Collections;
using System.Collections.Generic;
using Dataverse.Browser.Requests;

namespace Dataverse.Browser.Context
{
    internal class LastRequestsList : IEnumerable<InterceptedWebApiRequest>
    {
        private List<InterceptedWebApiRequest> InnerList { get; } = new List<InterceptedWebApiRequest>();
        private readonly object Locker = new object();

        public event EventHandler<InterceptedWebApiRequest> OnNewRequestIntercepted;
        public event EventHandler<InterceptedWebApiRequest> OnRequestUpdated;
        public event EventHandler OnHistoryCleared;


        public void AddRequest(InterceptedWebApiRequest request)
        {
            lock (this.Locker)
            {
                InnerList.Add(request);
                this.OnNewRequestIntercepted?.Invoke(this, request);
            }
        }

        public void TriggerUpdateRequest(InterceptedWebApiRequest request)
        {
            this.OnRequestUpdated?.Invoke(this, request);
        }

        public void Clear()
        {
            //TODO : le lock ici garantit que ça va bien se passer pour la liste interne
            // mais dans le treeview qui les affiche, à cause des invoke
            // on peut avoir des surprises 
            lock (this.Locker)
            {
                this.InnerList.Clear();
                this.OnHistoryCleared.Invoke(this, EventArgs.Empty);
            }
        }

        IEnumerator<InterceptedWebApiRequest> IEnumerable<InterceptedWebApiRequest>.GetEnumerator()
        {
            return this.InnerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<InterceptedWebApiRequest>)this).GetEnumerator();
        }
    }
}
