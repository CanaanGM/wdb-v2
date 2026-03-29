using Api.Features.Equipments.Commands.CreateEquipment;
using Api.Features.Equipments.Commands.CreateEquipmentsBulk;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Queries.GetAllEquipments;
using Api.Features.Equipments.Queries.GetEquipmentByName;
using Api.Features.Equipments.Queries.SearchEquipments;
using Api.Features.Equipments.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Features.Equipments;

[ApiController]
[Route("api/[controller]")]
public sealed class EquipmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<EquipmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EquipmentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var equipments = await sender.Send(new GetAllEquipmentsQuery(), cancellationToken);
        return Ok(equipments);
    }

    [HttpGet("search/{searchTerm}")]
    [ProducesResponseType<List<EquipmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<EquipmentResponse>>> Search(string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term is required.");
        }

        var equipments = await sender.Send(new SearchEquipmentsQuery(searchTerm), cancellationToken);
        return Ok(equipments);
    }

    [HttpGet("{name}")]
    [ProducesResponseType<EquipmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentResponse>> GetByName(string name, CancellationToken cancellationToken)
    {
        var equipment = await sender.Send(new GetEquipmentByNameQuery(name), cancellationToken);

        if (equipment is null)
        {
            return NotFound();
        }

        return Ok(equipment);
    }

    [HttpPost]
    [ProducesResponseType<EquipmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipmentResponse>> Create(
        [FromBody] CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateEquipmentCommand(request), cancellationToken);

        if (result.ResultType == CreateEquipmentResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == CreateEquipmentResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        if (result.Equipment is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Equipment creation failed.");
        }

        return CreatedAtAction(nameof(GetByName), new { name = result.Equipment.Name }, result.Equipment);
    }

    [HttpPost("bulk")]
    [ProducesResponseType<CreateEquipmentsBulkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateEquipmentsBulkResponse>> CreateBulk(
        [FromBody]
        [MinLength(1)]
        List<CreateEquipmentRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateEquipmentsBulkCommand(requests), cancellationToken);

        if (result.ResultType == CreateEquipmentsBulkResultType.ValidationError)
        {
            return BadRequest(result.Error);
        }

        if (result.ResultType == CreateEquipmentsBulkResultType.Conflict)
        {
            return Conflict(result.Error);
        }

        return Ok(new CreateEquipmentsBulkResponse
        {
            CreatedCount = result.CreatedCount
        });
    }
}
