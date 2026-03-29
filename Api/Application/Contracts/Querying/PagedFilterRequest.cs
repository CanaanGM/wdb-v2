using System.ComponentModel.DataAnnotations;

namespace Api.Application.Contracts.Querying;

public abstract class PagedFilterRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(200)]
    public string? Search { get; set; }
}
