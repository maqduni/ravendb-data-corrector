//-----------------------------------------------------------------------
// <copyright file="HiLoKeyGenerator.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;
using Raven.NewClient.Commands;
using Sparrow.Json;

namespace Raven.NewClient.Client.Document
{
    /// <summary>
    /// Generate hilo numbers against a RavenDB document
    /// </summary>
    public class HiLoKeyGenerator : HiLoKeyGeneratorBase
    {
        private readonly object generatorLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="HiLoKeyGenerator"/> class.
        /// </summary>
        public HiLoKeyGenerator(string tag, DocumentStore store, string dbName, string identityPartsSeparator)
            : base(tag, store, dbName, identityPartsSeparator)
        {
        }

        /// <summary>
        /// Generates the document key.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public string GenerateDocumentKey(object entity)
        {
            return GetDocumentKeyFromId(NextId());
        }

        ///<summary>
        /// Create the next id (numeric)
        ///</summary>
        public long NextId()
        {
            while (true)
            {
                var myRange = Range; // thread safe copy

                var current = Interlocked.Increment(ref myRange.Current);
                if (current <= myRange.Max)
                    return current;

                lock (generatorLock)
                {
                    if (Range != myRange)
                        // Lock was contended, and the max has already been changed. Just get a new id as usual.
                        continue;

                    Range = GetNextRange();
                }
            }
        }

        private RangeValue GetNextRange()
        {
            var hiloCommand = new NextHiLoCommand
            {
                Tag = _tag,
                LastBatchSize = _lastBatchSize,
                LastRangeAt = _lastRangeDate,
                IdentityPartsSeparator = _identityPartsSeparator,
                LastRangeMax = Range.Max
            };

            var re = _store.GetRequestExecuter(_dbName);
            JsonOperationContext context;

            using (re.ContextPool.AllocateOperationContext(out context))
            {
                re.Execute(hiloCommand, context);
            }
                                      
            _prefix = hiloCommand.Result.Prefix;
            _lastRangeDate = hiloCommand.Result.LastRangeAt;
            _lastBatchSize = hiloCommand.Result.LastSize;

            return new RangeValue(hiloCommand.Result.Low, hiloCommand.Result.High);
        }

        public void ReturnUnusedRange()
        {
            var returnCommand = new HiLoReturnCommand()
            {
                Tag = _tag,
                End = Range.Max,
                Last = Range.Current
            };

            var re = _store.GetRequestExecuter(_dbName);
            JsonOperationContext context;

            using (re.ContextPool.AllocateOperationContext(out context))
            {
                re.Execute(returnCommand, context);
            }
        }
    }
}
