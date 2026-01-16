using Jellyfin.Plugin.Consistart.Services.Artwork;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Services.Artwork;

public class ArtworkCandidateServiceTests
{
    #region Helper Methods

    private static ArtworkCandidateService CreateService(
        ILogger<ArtworkCandidateService>? logger = null,
        IEnumerable<IArtworkCandidateGenerator>? generators = null
    )
    {
        logger ??= Substitute.For<ILogger<ArtworkCandidateService>>();
        generators ??= [];

        return new ArtworkCandidateService(logger, generators);
    }

    private static Movie CreateBaseItem(string id = "test-item")
    {
        var item = new Movie { Id = Guid.Parse("00000000-0000-0000-0000-000000000001") };
        return item;
    }

    private static IArtworkCandidateGenerator CreateGenerator(
        bool canHandle = true,
        IReadOnlyList<ArtworkCandidateDto>? candidates = null
    )
    {
        var generator = Substitute.For<IArtworkCandidateGenerator>();
        generator.CanHandle(Arg.Any<BaseItem>(), Arg.Any<ImageType>()).Returns(canHandle);
        generator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(candidates ?? []));

        return generator;
    }

    private static ArtworkCandidateDto CreateCandidate(
        string id = "candidate-1",
        string url = "https://example.com/image.jpg",
        int? width = 500,
        int? height = 750,
        string? language = null
    )
    {
        return new ArtworkCandidateDto(id, url, width, height, language);
    }

    #endregion

    #region Basic Functionality Tests

    [Fact]
    public async Task GetCandidatesAsync_with_matching_generator_returns_candidates()
    {
        var candidate1 = CreateCandidate("cand1", "https://example.com/img1.jpg", 500, 750);
        var candidate2 = CreateCandidate("cand2", "https://example.com/img2.jpg", 600, 900);
        var candidates = new[] { candidate1, candidate2 };

        var generator = CreateGenerator(canHandle: true, candidates: candidates);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Equal(2, result.Count);
        Assert.Equal("cand1", result[0].Id);
        Assert.Equal("cand2", result[1].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_can_handle_with_correct_parameters()
    {
        var generator = CreateGenerator(canHandle: true);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var imageType = ImageType.Primary;

        await service.GetCandidatesAsync(item, imageType);

        generator.Received(1).CanHandle(item, imageType);
    }

    [Fact]
    public async Task GetCandidatesAsync_calls_get_candidates_async_with_correct_parameters()
    {
        var generator = CreateGenerator(canHandle: true);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var imageType = ImageType.Primary;
        var cts = new CancellationTokenSource();

        await service.GetCandidatesAsync(item, imageType, cts.Token);

        await generator.Received(1).GetCandidatesAsync(item, imageType, cts.Token);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_default_cancellation_token_passes_default_token()
    {
        var generator = CreateGenerator(canHandle: true);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();

        await service.GetCandidatesAsync(item, ImageType.Primary);

        await generator.Received(1).GetCandidatesAsync(item, ImageType.Primary, default);
    }

    #endregion

    #region Generator Selection Tests

    [Fact]
    public async Task GetCandidatesAsync_with_multiple_generators_selects_first_matching()
    {
        var matchingGenerator = CreateGenerator(
            canHandle: true,
            candidates: [CreateCandidate("match")]
        );
        var nonMatchingGenerator = CreateGenerator(canHandle: false);

        var service = CreateService(generators: [nonMatchingGenerator, matchingGenerator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("match", result[0].Id);

        // Verify that only the first generator was called for CanHandle
        nonMatchingGenerator.Received(1).CanHandle(item, ImageType.Primary);
        matchingGenerator.Received(1).CanHandle(item, ImageType.Primary);

        // But only the matching generator should be called for GetCandidatesAsync
        await nonMatchingGenerator
            .DidNotReceive()
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            );
        await matchingGenerator.Received(1).GetCandidatesAsync(item, ImageType.Primary, default);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_different_image_types_selects_appropriate_generator()
    {
        var primaryGenerator = Substitute.For<IArtworkCandidateGenerator>();
        primaryGenerator.CanHandle(Arg.Any<BaseItem>(), ImageType.Primary).Returns(true);
        primaryGenerator.CanHandle(Arg.Any<BaseItem>(), ImageType.Backdrop).Returns(false);
        primaryGenerator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                ImageType.Primary,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)[CreateCandidate("primary")])
            );

        var backdropGenerator = Substitute.For<IArtworkCandidateGenerator>();
        backdropGenerator.CanHandle(Arg.Any<BaseItem>(), ImageType.Primary).Returns(false);
        backdropGenerator.CanHandle(Arg.Any<BaseItem>(), ImageType.Backdrop).Returns(true);
        backdropGenerator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                ImageType.Backdrop,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult((IReadOnlyList<ArtworkCandidateDto>)[CreateCandidate("backdrop")])
            );

        var service = CreateService(generators: [primaryGenerator, backdropGenerator]);

        var item = CreateBaseItem();

        var primaryResult = await service.GetCandidatesAsync(item, ImageType.Primary);
        Assert.Single(primaryResult);
        Assert.Equal("primary", primaryResult[0].Id);

        var backdropResult = await service.GetCandidatesAsync(item, ImageType.Backdrop);
        Assert.Single(backdropResult);
        Assert.Equal("backdrop", backdropResult[0].Id);
    }

    #endregion

    #region No Generator Found Tests

    [Fact]
    public async Task GetCandidatesAsync_with_no_generators_returns_empty_list()
    {
        var service = CreateService(generators: []);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_no_matching_generator_returns_empty_list()
    {
        var generator = CreateGenerator(canHandle: false);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_no_matching_generator_logs_warning()
    {
        var logger = Substitute.For<ILogger<ArtworkCandidateService>>();
        var generator = CreateGenerator(canHandle: false);
        var service = new ArtworkCandidateService(logger, [generator]);

        var item = CreateBaseItem();
        var itemId = item.Id;

        await service.GetCandidatesAsync(item, ImageType.Primary);

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("No artwork candidate generator found")),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_with_no_matching_generator_logs_correct_item_id()
    {
        var logger = Substitute.For<ILogger<ArtworkCandidateService>>();
        var generator = CreateGenerator(canHandle: false);
        var service = new ArtworkCandidateService(logger, [generator]);

        var item = new Movie { Id = Guid.Parse("12345678-1234-1234-1234-123456789012") };

        await service.GetCandidatesAsync(item, ImageType.Primary);

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("12345678-1234-1234-1234-123456789012")),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_with_no_matching_generator_logs_correct_image_type()
    {
        var logger = Substitute.For<ILogger<ArtworkCandidateService>>();
        var generator = CreateGenerator(canHandle: false);
        var service = new ArtworkCandidateService(logger, [generator]);

        var item = CreateBaseItem();

        await service.GetCandidatesAsync(item, ImageType.Backdrop);

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Backdrop")),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    #endregion

    #region Empty Candidate List Tests

    [Fact]
    public async Task GetCandidatesAsync_with_generator_returning_empty_list_returns_empty()
    {
        var generator = CreateGenerator(canHandle: true, candidates: []);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_preserves_order_of_candidates()
    {
        var candidates = new[]
        {
            CreateCandidate("first", width: 100),
            CreateCandidate("second", width: 200),
            CreateCandidate("third", width: 300),
        };

        var generator = CreateGenerator(canHandle: true, candidates: candidates);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Equal(3, result.Count);
        Assert.Equal("first", result[0].Id);
        Assert.Equal("second", result[1].Id);
        Assert.Equal("third", result[2].Id);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task GetCandidatesAsync_propagates_generator_exception()
    {
        var generator = Substitute.For<IArtworkCandidateGenerator>();
        generator.CanHandle(Arg.Any<BaseItem>(), Arg.Any<ImageType>()).Returns(true);
        var expectedException = new InvalidOperationException("Generator error");
        generator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<IReadOnlyList<ArtworkCandidateDto>>(expectedException));

        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCandidatesAsync(item, ImageType.Primary)
        );

        Assert.Equal("Generator error", ex.Message);
    }

    [Fact]
    public async Task GetCandidatesAsync_when_generator_throws_does_not_swallow_exception()
    {
        var generator = Substitute.For<IArtworkCandidateGenerator>();
        generator.CanHandle(Arg.Any<BaseItem>(), Arg.Any<ImageType>()).Returns(true);
        generator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromException<IReadOnlyList<ArtworkCandidateDto>>(
                    new ArgumentException("Invalid argument")
                )
            );

        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetCandidatesAsync(item, ImageType.Primary)
        );
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetCandidatesAsync_respects_cancellation_token()
    {
        var cts = new CancellationTokenSource();
        var generator = Substitute.For<IArtworkCandidateGenerator>();
        generator.CanHandle(Arg.Any<BaseItem>(), Arg.Any<ImageType>()).Returns(true);

        var operationStarted = new TaskCompletionSource();
        async Task<IReadOnlyList<ArtworkCandidateDto>> CancelledOperation(
            BaseItem _,
            ImageType __,
            CancellationToken ct
        )
        {
            operationStarted.SetResult();
            try
            {
                // Simulate long-running operation
                await Task.Delay(Timeout.Infinite, ct);
                return [];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        generator
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(x =>
                CancelledOperation(
                    x.ArgAt<BaseItem>(0),
                    x.ArgAt<ImageType>(1),
                    x.ArgAt<CancellationToken>(2)
                )
            );

        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var task = service.GetCandidatesAsync(item, ImageType.Primary, cts.Token);

        await operationStarted.Task;
        cts.Cancel();

        // TaskCanceledException is a subclass of OperationCanceledException
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetCandidatesAsync_with_single_generator_uses_it()
    {
        var candidate = CreateCandidate("only");
        var generator = CreateGenerator(canHandle: true, candidates: [candidate]);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("only", result[0].Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_many_generators_first_match_wins()
    {
        var gen1 = CreateGenerator(canHandle: false);
        var gen2 = CreateGenerator(canHandle: false);
        var gen3 = CreateGenerator(canHandle: true, candidates: [CreateCandidate("winner")]);
        var gen4 = CreateGenerator(canHandle: true, candidates: [CreateCandidate("never-called")]);

        var service = CreateService(generators: [gen1, gen2, gen3, gen4]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("winner", result[0].Id);

        // Verify gen4 was never consulted
        await gen4.DidNotReceive()
            .GetCandidatesAsync(
                Arg.Any<BaseItem>(),
                Arg.Any<ImageType>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetCandidatesAsync_with_candidates_with_all_optional_fields_preserves_them()
    {
        var candidate = CreateCandidate(
            id: "full-candidate",
            url: "https://example.com/full.jpg",
            width: 1920,
            height: 1080,
            language: "en"
        );

        var generator = CreateGenerator(canHandle: true, candidates: [candidate]);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal("full-candidate", dto.Id);
        Assert.Equal("https://example.com/full.jpg", dto.Url);
        Assert.Equal(1920, dto.Width);
        Assert.Equal(1080, dto.Height);
        Assert.Equal("en", dto.Language);
    }

    [Fact]
    public async Task GetCandidatesAsync_with_candidates_with_null_optional_fields_preserves_them()
    {
        var candidate = new ArtworkCandidateDto(
            "minimal-candidate",
            "https://example.com/minimal.jpg",
            null,
            null,
            null
        );

        var generator = CreateGenerator(canHandle: true, candidates: [candidate]);
        var service = CreateService(generators: [generator]);

        var item = CreateBaseItem();
        var result = await service.GetCandidatesAsync(item, ImageType.Primary);

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal("minimal-candidate", dto.Id);
        Assert.Equal("https://example.com/minimal.jpg", dto.Url);
        Assert.Null(dto.Width);
        Assert.Null(dto.Height);
        Assert.Null(dto.Language);
    }

    #endregion

    #region Real Item Type Tests

    [Fact]
    public async Task GetCandidatesAsync_works_with_movie_item()
    {
        var movie = new Movie { Id = Guid.Parse("12345678-1234-1234-1234-123456789012") };

        var candidates = new[] { CreateCandidate("movie-poster") };
        var generator = CreateGenerator(canHandle: true, candidates: candidates);
        var service = CreateService(generators: [generator]);

        var result = await service.GetCandidatesAsync(movie, ImageType.Primary);

        Assert.Single(result);
        Assert.Equal("movie-poster", result[0].Id);
    }

    #endregion
}
