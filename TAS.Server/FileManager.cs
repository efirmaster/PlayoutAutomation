﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using Newtonsoft.Json;
using TAS.Remoting.Server;

namespace TAS.Server
{
    public class FileManager: DtoBase, IFileManager
    {
        [JsonProperty]
        private readonly string Dummy; // at  least one property should be serialized to resolve references
        private SynchronizedCollection<IFileOperation> _queueSimpleOperation = new SynchronizedCollection<IFileOperation>();
        private SynchronizedCollection<IFileOperation> _queueConvertOperation = new SynchronizedCollection<IFileOperation>();
        private SynchronizedCollection<IFileOperation> _queueExportOperation = new SynchronizedCollection<IFileOperation>();
        private bool _isRunningSimpleOperation = false;
        private bool _isRunningConvertOperation = false;
        private bool _isRunningExportOperation = false;

        public event EventHandler<FileOperationEventArgs> OperationAdded;
        public event EventHandler<FileOperationEventArgs> OperationCompleted;

        public IConvertOperation CreateConvertOperation() { return new ConvertOperation(); }
        public ILoudnessOperation CreateLoudnessOperation() { return new LoudnessOperation(); }
        public IFileOperation CreateFileOperation() { return new FileOperation(); }

        public TempDirectory TempDirectory;
        public decimal VolumeReferenceLoudness;
        public IEnumerable<IFileOperation> GetOperationQueue()
        {
            List<IFileOperation> retList;
            lock (_queueSimpleOperation.SyncRoot)
                retList = new List<IFileOperation>(_queueSimpleOperation);
            lock (_queueConvertOperation.SyncRoot)
                retList.AddRange(_queueConvertOperation);
            lock (_queueExportOperation.SyncRoot)
                retList.AddRange(_queueExportOperation);
            return retList;
        }

        public void QueueList(IEnumerable<IFileOperation> operationList, bool toTop = false)
        {
            foreach (var operation in operationList)
                Queue(operation, toTop);
        }

        public void Queue(IFileOperation operation, bool toTop = false)
        {
            ((FileOperation)operation).Owner = this;
            FileOperation op = operation as FileOperation;
            op.ScheduledTime = DateTime.UtcNow;
            op.OperationStatus = FileOperationStatus.Waiting;

            if ((operation.Kind == TFileOperationKind.Copy || operation.Kind == TFileOperationKind.Move || operation.Kind == TFileOperationKind.Convert)
                && operation.DestMedia != null)
                operation.DestMedia.MediaStatus = TMediaStatus.CopyPending;
            if (operation.Kind == TFileOperationKind.Convert)
            {
                lock (_queueConvertOperation.SyncRoot)
                {
                    if (toTop)
                        _queueConvertOperation.Insert(0, operation);
                    else
                        _queueConvertOperation.Add(operation);
                    if (!_isRunningConvertOperation)
                    {
                        _isRunningConvertOperation = true;
                        ThreadPool.QueueUserWorkItem(o => _runOperation(_queueConvertOperation, ref _isRunningConvertOperation));
                    }
                }
            }
            if (operation.Kind == TFileOperationKind.Export)
            {
                lock (_queueExportOperation.SyncRoot)
                    {
                        if (toTop)
                            _queueExportOperation.Insert(0, operation);
                        else
                            _queueExportOperation.Add(operation);
                        if (!_isRunningExportOperation)
                        {
                            _isRunningExportOperation = true;
                            ThreadPool.QueueUserWorkItem(o => _runOperation(_queueExportOperation, ref _isRunningExportOperation));
                        }
                    }
            }
            if (operation.Kind == TFileOperationKind.Copy
                || operation.Kind == TFileOperationKind.Delete
                || operation.Kind == TFileOperationKind.Loudness
                || operation.Kind == TFileOperationKind.Move)
            {
                lock (_queueSimpleOperation.SyncRoot)
                    {
                        if (toTop)
                            _queueSimpleOperation.Insert(0, operation);
                        else
                            _queueSimpleOperation.Add(operation);
                        if (!_isRunningSimpleOperation)
                        {
                            _isRunningSimpleOperation = true;
                            ThreadPool.QueueUserWorkItem(o => _runOperation(_queueSimpleOperation, ref _isRunningSimpleOperation));
                        }
                    }
            }
            NotifyOperation(OperationAdded, operation);
        }

        public void CancelPending()
        {
            lock (_queueSimpleOperation.SyncRoot)
                _queueSimpleOperation.ToList().ForEach(op => { if (op.OperationStatus == FileOperationStatus.Waiting) op.Abort(); });
            lock (_queueConvertOperation.SyncRoot)
                _queueSimpleOperation.ToList().ForEach(op => { if (op.OperationStatus == FileOperationStatus.Waiting) op.Abort(); });
            lock (_queueExportOperation.SyncRoot)
                _queueExportOperation.ToList().ForEach(op => { if (op.OperationStatus == FileOperationStatus.Waiting) op.Abort(); });
        }

        private void _runOperation(SynchronizedCollection<IFileOperation> queue, ref bool queueRunningIndicator)
        {
            FileOperation op;
            lock (queue.SyncRoot)
                op = queue.FirstOrDefault() as FileOperation;
            while (op != null)
            {
                queue.Remove(op);
                if (!op.Aborted)
                {
                    if (op.Do())
                        NotifyOperation(OperationCompleted, op);
                    else
                    {
                        if (op.TryCount > 0)
                        {
                            System.Threading.Thread.Sleep(500);
                            queue.Add(op);
                        }
                        else
                        {
                            op.Fail();
                            NotifyOperation(OperationCompleted, op);
                            if (op.DestMedia != null)
                                op.DestMedia.Delete();
                        }
                    }
                }
                lock (queue.SyncRoot)
                    op = queue.FirstOrDefault() as FileOperation;
            }
            lock (queue.SyncRoot)
                queueRunningIndicator = false;
        }

        private void NotifyOperation(EventHandler<FileOperationEventArgs> handler, IFileOperation operation)
        {
            handler?.Invoke(this, new FileOperationEventArgs(operation));
        }
    }


}
