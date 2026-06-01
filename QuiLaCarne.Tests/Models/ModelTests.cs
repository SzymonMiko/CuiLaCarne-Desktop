using QuiLaCarne.Models;
using Xunit;

namespace QuiLaCarne.Tests.Models;

public sealed class ModelTests
{
    [Fact]
    public void IngredientDisplayName_UsesNameWhenPresent()
    {
        var ingredient = new Ingredients
        {
            Token = "EGG_ING",
            Name = "Jajka"
        };

        Assert.Equal("Jajka", ingredient.DisplayName);
    }

    [Fact]
    public void IngredientDisplayName_FallsBackToTokenWhenNameIsBlank()
    {
        var ingredient = new Ingredients
        {
            Token = "EGG_ING",
            Name = ""
        };

        Assert.Equal("EGG_ING", ingredient.DisplayName);
    }

    [Fact]
    public void BaseEntity_OnCreateSetsCreatedAndUpdatedTimestamps()
    {
        var user = new Users();

        user.OnCreate();

        Assert.NotEqual(default, user.CreatedAt);
        Assert.NotEqual(default, user.UpdatedAt);
        Assert.True(user.UpdatedAt >= user.CreatedAt);
    }

    [Fact]
    public void BaseEntity_OnUpdateChangesUpdatedTimestampOnly()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");
        var user = new Users
        {
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        user.OnUpdate();

        Assert.Equal(createdAt, user.CreatedAt);
        Assert.True(user.UpdatedAt > createdAt);
    }

    [Fact]
    public void OrderItem_DefaultQuantityIsOne()
    {
        var item = new OrderItems();

        Assert.Equal(1, item.Quantity);
    }
}
