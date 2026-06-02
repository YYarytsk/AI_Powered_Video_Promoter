using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IVideoOptimizationService
{
    Task<OptimizeVideoResponse> OptimizeAsync(OptimizeVideoRequest request, CancellationToken cancellationToken = default);
}
