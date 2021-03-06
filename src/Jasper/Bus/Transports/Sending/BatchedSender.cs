﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Jasper.Bus.Logging;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports.Tcp;
using Jasper.Bus.Transports.Util;

namespace Jasper.Bus.Transports.Sending
{
    public class BatchedSender : ISender
    {
        public Uri Destination { get; }

        private readonly ISenderProtocol _protocol;
        private readonly CancellationToken _cancellation;
        private readonly CompositeTransportLogger _logger;
        private ISenderCallback _callback;
        private ActionBlock<OutgoingMessageBatch> _sender;
        private BatchingBlock<Envelope> _batching;
        private int _queued = 0;
        private ActionBlock<Envelope> _serializing;
        private TransformBlock<Envelope[], OutgoingMessageBatch> _batchWriting;

        public BatchedSender(Uri destination, ISenderProtocol protocol, CancellationToken cancellation, CompositeTransportLogger logger)
        {
            Destination = destination;
            _protocol = protocol;
            _cancellation = cancellation;
            _logger = logger;
        }

        public void Start(ISenderCallback callback)
        {
            _callback = callback;

            _sender = new ActionBlock<OutgoingMessageBatch>(SendBatch, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                CancellationToken = _cancellation
            });

            _serializing = new ActionBlock<Envelope>(e =>
            {
                try
                {
                    e.EnsureData();
                    _batching.Post(e);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, message:$"Error while trying to serialize envelope {e}");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellation
            });


            _batchWriting = new TransformBlock<Envelope[], OutgoingMessageBatch>(
                envelopes =>
                {
                    var batch = new OutgoingMessageBatch(Destination, envelopes);
                    _queued += batch.Messages.Count;
                    return batch;
                });

            _batchWriting.LinkTo(_sender);

            _batching = new BatchingBlock<Envelope>(200, _batchWriting, _cancellation);


        }

        public int QueuedCount => _queued + _batching.ItemCount;

        public bool Latched { get; private set; }
        public async Task LatchAndDrain()
        {
            Latched = true;

            _sender.Complete();
            _serializing.Complete();
            _batchWriting.Complete();
            _batching.Complete();


            _logger.CircuitBroken(Destination);


            await _sender.Completion;
            await _serializing.Completion;
            await _batchWriting.Completion;
            await _batching.Completion;

        }

        public void Unlatch()
        {
            _logger.CircuitResumed(Destination);

            Start(_callback);
            Latched = false;
        }

        public Task Ping()
        {
            var batch = OutgoingMessageBatch.ForPing(Destination);
            return _protocol.SendBatch(_callback,batch);
        }

        public async Task SendBatch(OutgoingMessageBatch batch)
        {
            if (_cancellation.IsCancellationRequested) return;

            try
            {
                if (Latched)
                {
                    await _callback.SenderIsLatched(batch);
                }
                else
                {
                    await _protocol.SendBatch(_callback, batch);
                    _logger.OutgoingBatchSucceeded(batch);
                }

            }
            catch (Exception e)
            {
                batchSendFailed(batch, e);
            }

            finally
            {
                _queued -= batch.Messages.Count;
            }
        }

        private void batchSendFailed(OutgoingMessageBatch batch, Exception exception)
        {
            _callback.ProcessingFailure(batch, exception);
        }

        public Task Enqueue(Envelope message)
        {
            if (_batching == null) throw new InvalidOperationException("This agent has not been started");

            _serializing.Post(message);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _serializing?.Complete();
            _sender?.Complete();
            _batching?.Dispose();
        }
    }
}
