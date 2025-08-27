using Microsoft.SemanticKernel.Memory;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

public interface IMemoryAugmentationService
{
    /// <summary>
    /// Asynchronously generates a descriptive summary for the provided memory content.
    /// </summary>
    /// <param name="memoryContent">The content of the memory for which a description needs to be generated.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated memory description as a string.</returns>
    Task<string> GenerateMemoryDescriptionAsync(string memoryContent);

    /// <summary>
    /// Asynchronously generates a descriptive summary for a list of memories associated with the specified store identifier.
    /// </summary>
    /// <param name="storeIdentifier">The unique identifier of the store for which the description is being generated.</param>
    /// <param name="memories">A list of memories associated with the store, providing contextual information for description generation.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated store description as a string.</returns>
    Task<string> GenerateStoreDescriptionAsync(string storeIdentifier, List<Memory> memories);
}
