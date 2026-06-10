// Copyright 2024 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using NServiceBus;

using PPWCode.Server.Core.RequestContext.Interfaces;

namespace PPWCode.Server.Core.NServiceBus
{
    public abstract class MessageHandler<TMessage> : IHandleMessages<TMessage>
    {
        [CanBeNull]
        private ILogger _logger;

        protected MessageHandler([NotNull] IRequestContext requestContext)
        {
            RequestContext = requestContext;
        }

        [NotNull]
        public IRequestContext RequestContext { get; }

        [NotNull]
        [UsedImplicitly]
        public ILogger Logger
        => _logger ??= PPWLogging.GetLogger(GetType());

        [CanBeNull]
        [UsedImplicitly]
        public IMessageContextAccessor MessageContextAccessor { get; set; }

        /// <inheritdoc />
        public abstract Task Handle(TMessage message, IMessageHandlerContext context);
    }
}
