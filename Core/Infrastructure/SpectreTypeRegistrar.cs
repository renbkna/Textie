using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Textie.Core.Infrastructure
{
    public sealed class SpectreTypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceProvider _provider;

        public SpectreTypeRegistrar(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ITypeResolver Build()
        {
            return new SpectreTypeResolver(_provider);
        }

        public void Register(Type service, Type implementation)
        {
            // Services are registered via the host's DI container.
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // Not required because DI container owns registrations.
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // Not required for our composition root.
        }
    }

    public sealed class SpectreTypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceScope _scope;

        public SpectreTypeResolver(IServiceProvider provider)
        {
            _scope = provider.CreateScope();
        }

        public object? Resolve(Type? type)
        {
            return type == null ? null : _scope.ServiceProvider.GetService(type);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
