using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PartTwoProg.Controllers;
using PartTwoProg.Data;
using PartTwoProg.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Claim = PartTwoProg.Models.Claim;

public class ClaimsControllerTests
{
    private readonly ClaimsController _controller;
    private readonly ApplicationDbContext _dbContext;

    public ClaimsControllerTests()
    {
        // Unique database name using Guid to ensure each test uses a fresh database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _controller = new ClaimsController(_dbContext);
    }

    [Fact]
    public async Task SubmitClaim_ValidFile_ReturnsRedirectToAction()
    {
        // Arrange
        var claim = new Claim { Id = 1, Status = "Pending", AdditionalNotes = "Note", LecturerName = "Lecturer", SupportingDocument = "doc.pdf" };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SubmitClaim(claim, fileMock.Object) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ClaimSubmitted", result.ActionName);
    }
    [Fact]
    public async Task SubmitClaim_InvalidFileType_ReturnsViewWithModelError()
    {
        // Arrange
        var claim = new Claim { Id = 1, Status = "Pending" };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _controller.SubmitClaim(claim, fileMock.Object) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ViewPendingClaims_ReturnsViewWithPendingClaims()
    {
        // Arrange
        var pendingClaims = new List<Claim>
    {
        new Claim
        {
            Id = 1,
            Status = "Pending",
            AdditionalNotes = "First claim note",
            LecturerName = "John Doe",
            SupportingDocument = "document1.pdf" // Set this property
        },
        new Claim
        {
            Id = 2,
            Status = "Pending",
            AdditionalNotes = "Second claim note",
            LecturerName = "Jane Smith",
            SupportingDocument = "document2.pdf" // Set this property
        }
    };

        _dbContext.Claims.AddRange(pendingClaims);
        await _dbContext.SaveChangesAsync(); // Ensure changes are saved

        // Act
        var result = await _controller.ViewPendingClaims() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsAssignableFrom<List<Claim>>(result.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task ApproveClaim_ExistingClaim_ReturnsRedirectToAction()
    {
        // Arrange
        var claim = new Claim
        {
            Id = 1,
            Status = "Pending",
            AdditionalNotes = "Sample additional notes",  // Providing required field
            LecturerName = "Jane Doe",                    // Providing required field
            SupportingDocument = "test.pdf"               // Providing required field
        };

        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ApproveClaim(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ViewPendingClaims", result.ActionName);
        Assert.Equal("Approved", claim.Status);
    }

    [Fact]
    public async Task RejectClaim_ExistingClaim_ReturnsRedirectToAction()
    {
        // Arrange
        var claim = new Claim
        {
            Id = 1,
            Status = "Pending",
            AdditionalNotes = "Sample notes",     // Providing required field
            LecturerName = "John Doe",            // Providing required field
            SupportingDocument = "test.pdf"       // Providing required field
        };

        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.RejectClaim(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ViewPendingClaims", result.ActionName);
        Assert.Equal("Rejected", claim.Status);
    }

    [Fact]
    public async Task DeleteClaim_ExistingClaim_ReturnsRedirectToAction()
    {
        // Arrange
        var claim = new Claim
        {
            Id = 1,
            Status = "Pending",
            AdditionalNotes = "Sample notes",      // Providing required field
            LecturerName = "John Doe",             // Providing required field
            SupportingDocument = "test.pdf"        // Providing required field
        };

        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteClaim(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TrackClaims", result.ActionName);
        Assert.Null(await _dbContext.Claims.FindAsync(1)); // Ensure it was deleted
    }
}
