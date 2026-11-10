using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Operational staff documents (recipes, manuals, policies). Staff and Admin only.</summary>
[ApiController]
[Route("api/documents")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public sealed class DocumentsController(IDocumentService documents) : ControllerBase
{
    /// <summary>Uploads a document (multipart/form-data, field name "file").</summary>
    /// <response code="201">Stored in the staff-docs container.</response>
    /// <response code="400">Empty file, too large (10 MB) or disallowed type.</response>
    [HttpPost, Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentInfoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentInfoDto>> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var info = await documents.UploadAsync(file.FileName, file.ContentType, file.Length, stream, ct);
        return Created($"/api/documents/{Uri.EscapeDataString(info.FileName)}", info);
    }

    /// <summary>Lists stored documents with size and last-modified date.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentInfoDto>>> List(CancellationToken ct)
        => Ok(await documents.ListAsync(ct));

    /// <summary>Downloads a document as a stream.</summary>
    /// <param name="fileName">Exact file name, e.g. espresso-recipe.pdf</param>
    /// <response code="200">The file.</response>
    /// <response code="404">No such document.</response>
    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName, CancellationToken ct)
    {
        var doc = await documents.DownloadAsync(fileName, ct);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    /// <summary>Deletes a document.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No such document.</response>
    [HttpDelete("{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string fileName, CancellationToken ct)
    {
        await documents.DeleteAsync(fileName, ct);
        return NoContent();
    }
}