namespace Sitecore.Support.Commerce.Engine.Connect.Pipelines
{
  using System;

  using Sitecore.Commerce.Pipelines;
  using Sitecore.Commerce.Services;
  using Sitecore.Diagnostics;
  using Sitecore.Pipelines;

  /// <summary>
  /// Defines the PipelineUtility static class.
  /// </summary>
  public static class FixedPipelineUtility
  {
    /// <summary>
    /// The cart line item item id delimiter
    /// </summary>
    public const string CartLineItemIdDelimiter = "|";

    /// <summary>
    /// The sellable item identifier identifier delimiter
    /// </summary>
    public const string SellableItemIdIdDelimiter = ",";

    /// <summary>
    /// The sellable items identifier identifier delimiter
    /// </summary>
    public const string SellableItemsIdIdDelimiter = "|";

    /// <summary>
    /// Runs the pipeline Connect style.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <param name="request">The request.</param>
    /// <returns>
    /// The pipeline result is returned by this method.
    /// </returns>
    public static TResult RunConnectPipeline<TRequest, TResult>(string pipelineName, TRequest request)
        where TRequest : ServiceProviderRequest
        where TResult : ServiceProviderResult, new()
    {
      var translateArgs = new ServicePipelineArgs(request, new TResult());

      CorePipeline.Run(pipelineName, translateArgs);

      return translateArgs.Result as TResult;
    }

    /// <summary>
    /// The create system message.
    /// </summary>
    /// <param name="ex">
    /// The ex.
    /// </param>
    /// <returns>
    /// The <see cref="SystemMessage"/>.
    /// </returns>
    internal static SystemMessage CreateSystemMessage(Exception ex)
    {
      var message = new SystemMessage { Message = ex.Message };
      if (ex.InnerException != null && !ex.Message.Equals(ex.InnerException.Message, StringComparison.OrdinalIgnoreCase))
      {
        message.Message += string.Concat(" - ", ex.InnerException.Message);
      }

      return message;
    }

    /// <summary>
    /// The create system message.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <returns>
    /// The <see cref="SystemMessage"/>.
    /// </returns>
    internal static SystemMessage CreateSystemMessage(string message)
    {
      return new SystemMessage { Message = message };
    }

    /// <summary>
    /// The validate arguments.
    /// </summary>
    /// <typeparam name="TRequest">
    /// The request
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result
    /// </typeparam>
    /// <param name="args">
    /// The args.
    /// </param>
    /// <param name="request">
    /// The request.
    /// </param>
    /// <param name="result">
    /// The result.
    /// </param>
    internal static void ValidateArguments<TRequest, TResult>(ServicePipelineArgs args, out TRequest request, out TResult result)
        where TRequest : ServiceProviderRequest
        where TResult : ServiceProviderResult
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(args.Request, "args.Request");
      Assert.ArgumentNotNull(args.Request.RequestContext, "args.Request.RequestContext");
      Assert.ArgumentNotNull(args.Result, "args.Result");

      request = args.Request as TRequest;
      result = args.Result as TResult;
      Assert.IsNotNull(request, "The parameter args.Request was not of the expected type.  Expected {0}.  Actual {1}.", typeof(TRequest).Name, args.Request.GetType().Name);
      Assert.IsNotNull(result, "The parameter args.Result was not of the expected type.  Expected {0}.  Actual {1}.", typeof(TResult).Name, args.Result.GetType().Name);
    }
  }
}

