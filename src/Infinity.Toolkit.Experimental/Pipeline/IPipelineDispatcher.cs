using Infinity.Toolkit.Mediator;
using System.Threading.Tasks.Dataflow;

namespace Infinity.Toolkit.Pipeline;

public interface IPipelineDispatcher<TIn, TOut>
{
    Task DispatchAsync(TIn input, CancellationToken cancellationToken = default);
}

public class PipelineDispatcher<TIn, TOut> : IPipelineDispatcher<TIn, TOut>
{
    private readonly IPipeline<TIn, TOut> pipeline;

    public PipelineDispatcher(IPipeline<TIn, TOut> pipeline)
    {
        this.pipeline = pipeline;
    }

    public Task DispatchAsync(TIn input, CancellationToken cancellationToken = default)
    {
        return pipeline.ExecuteAsync(input, cancellationToken);
    }
}

public interface IPipeline<TIn, TOut>
{
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}

public class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
{
    private readonly List<IDataflowBlock> pipelineSteps = [];

    public Pipeline<TIn, TOut> AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFunc)
    {
        var block = new TransformBlock<PipelineContext<TStepIn, TOut>, PipelineContext<TStepOut, TOut>>(context =>
        {
            try
            {
                return new PipelineContext<TStepOut, TOut>(stepFunc(context.Input), context.TaskCompletionSource);
            }
            catch (Exception ex)
            {
                context.TaskCompletionSource.SetException(ex);
                return new PipelineContext<TStepOut, TOut>(default!, context.TaskCompletionSource);
            }
        });

        if (pipelineSteps.Count > 0)
        {
            var previousBlock = pipelineSteps.Last() as ISourceBlock<PipelineContext<TStepIn, TOut>>;
            previousBlock!.LinkTo(block);
        }

        pipelineSteps.Add(block);

        return this;
    }

    public Pipeline<TIn, TOut> AddStep<TStepIn, TStepOut>(ICommandHandler<TStepIn> handler)
        where TStepIn : class, ICommand
    {
        return this;
    }

    public Pipeline<TIn, TOut> Build()
    {
        var setResultStep = new ActionBlock<PipelineContext<TOut, TOut>>(context =>
        {
            context.TaskCompletionSource.SetResult(context.Input);
        });

        var lastStep = pipelineSteps.Last() as ISourceBlock<PipelineContext<TOut, TOut>>;
        lastStep!.LinkTo(setResultStep);

        return this;
    }

    public async Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default)
    {
        var firstStep = pipelineSteps.First() as ITargetBlock<PipelineContext<TIn, TOut>>;
        var tcs = new TaskCompletionSource<TOut>();
        await firstStep!.SendAsync(new PipelineContext<TIn, TOut>(input, tcs));

        return await tcs.Task;
    }
}

public class PipelineContext<TIn, TOut>
{
    public PipelineContext(TIn input, TaskCompletionSource<TOut> taskCompletionSource)
    {
        Input = input;
        TaskCompletionSource = taskCompletionSource;
    }

    public TIn Input { get; set; }

    public TOut Output { get; set; }

    public TaskCompletionSource<TOut> TaskCompletionSource { get; } = new();
}

public static class PipelineExtensions
{
    public static Pipeline<TIn, TOut> CreatePipeline<TIn, TOut>()
    {
        return new();
    }

    public static Pipeline<TIn, TOut> AddStep<TIn, TOut>(this Pipeline<TIn, TOut> pipeline, Func<TIn, TOut> stepFunc)
    {
        return pipeline.AddStep(stepFunc);
    }

    //public static Pipeline<TIn, TOut> AddCommand<TIn, TOut>(this Pipeline<TIn, TOut>, TCommand command)
    //{
    //    return pipeline.AddStep(command.HandleAsync);
    //}
}
