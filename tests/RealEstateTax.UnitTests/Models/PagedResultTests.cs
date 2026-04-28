using FluentAssertions;
using RealEstateTax.Application.Common.Models;

namespace RealEstateTax.UnitTests.Models;

public class PagedResultTests
{
    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<string>
        {
            TotalCount = totalCount,
            PageSize = pageSize,
            Page = 1,
            Items = []
        };

        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void HasNextPage_True_WhenNotOnLastPage()
    {
        var result = new PagedResult<string> { TotalCount = 30, PageSize = 10, Page = 1, Items = [] };
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_False_WhenOnLastPage()
    {
        var result = new PagedResult<string> { TotalCount = 30, PageSize = 10, Page = 3, Items = [] };
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_False_WhenOnFirstPage()
    {
        var result = new PagedResult<string> { TotalCount = 30, PageSize = 10, Page = 1, Items = [] };
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_True_WhenNotOnFirstPage()
    {
        var result = new PagedResult<string> { TotalCount = 30, PageSize = 10, Page = 2, Items = [] };
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Result_Success_HasCorrectProperties()
    {
        var data = new { Name = "Test" };
        var result = Result<object>.Success(data);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(data);
        result.Error.Should().BeNull();
        result.ErrorCode.Should().BeNull();
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void Result_Failure_HasCorrectProperties()
    {
        var result = Result<string>.Failure("Something went wrong", "SERVER_ERROR");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Something went wrong");
        result.ErrorCode.Should().Be("SERVER_ERROR");
        result.Data.Should().BeNull();
    }

    [Fact]
    public void Result_NotFound_SetsNotFoundCode()
    {
        var result = Result<string>.NotFound("Property not found");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
        result.Error.Should().Be("Property not found");
    }

    [Fact]
    public void Result_Forbidden_SetsForbiddenCode()
    {
        var result = Result<string>.Forbidden("Access denied");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public void Result_ValidationFailure_SetsValidationErrorCode()
    {
        var errors = new[] { "Name is required", "Email is invalid" };
        var result = Result<string>.ValidationFailure(errors);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ValidationErrors.Should().BeEquivalentTo(errors);
    }
}
