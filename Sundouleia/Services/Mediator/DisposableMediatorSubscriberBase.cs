namespace Sundouleia.Services.Mediator;

public abstract class DisposableMediatorSubscriberBase : MediatorSubscriberBase, IDisposable
{
    protected DisposableMediatorSubscriberBase(ILogger logger, SundouleiaMediator mediator)
        : base(logger, mediator)
    { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Logger.LogTrace($"Disposing {GetType().Name} ({this})", LoggerType.Mediator);
        UnsubscribeAll();
    }
}
