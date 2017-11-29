﻿using System;
using System.Linq;
using System.Threading;
using Jasper.Bus.Transports.Configuration;
using Jasper.Bus.Transports.Sending;
using Jasper.Bus.Transports.Tcp;
using Jasper.Marten.Persistence.Resiliency;
using Marten;

namespace Jasper.Marten.Persistence
{
    public class MartenBackedRetryAgent : RetryAgent
    {
        private readonly IDocumentStore _store;
        private readonly OwnershipMarker _marker;

        public MartenBackedRetryAgent(IDocumentStore store, ISender sender, RetrySettings settings, OwnershipMarker marker) : base(sender, settings)
        {
            _store = store;
            _marker = marker;
        }

        public override void EnqueueForRetry(OutgoingMessageBatch batch)
        {
            try
            {
                using (var session = _store.LightweightSession())
                {
                    _marker.MarkOwnedByAnyNode(session, batch.Messages.ToArray());

                    foreach (var envelope in batch.Messages.Where(x => x.IsExpired()))
                    {
                        session.Delete(envelope);
                    }

                    session.SaveChanges();
                }
            }
            catch (Exception e)
            {
                // TODO -- FAR BETTER STRATEGY HERE!
                Thread.Sleep(100);
                EnqueueForRetry(batch);
            }
        }

        protected override void afterRestarting()
        {
            // Nothing here.
        }
    }
}