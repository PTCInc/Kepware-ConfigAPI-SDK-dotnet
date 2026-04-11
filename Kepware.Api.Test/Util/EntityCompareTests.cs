using Kepware.Api.Model;
using Kepware.Api.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Test.Util
{
    public class EntityCompareTests
    {
        private class UidTestEntity : NamedUidEntity
        {
            protected override string UniqueIdKey => nameof(UniqueId);

            public UidTestEntity(string name, long uuid)
                : base(name)
            {
                SetDynamicProperty(UniqueIdKey, uuid);
            }
        }

        #region Helper-Methoden

        private static EntityCollection<UidTestEntity> CreateCollection(params (string Name, long UniqueId)[] entities)
        {
            return new EntityCollection<UidTestEntity>(
                entities.Select(e => new UidTestEntity(e.Name, e.UniqueId)).ToList()
            );
        }

        #endregion

        #region Compare - Einzelne Entitäten

        [Fact]
        public void Compare_ShouldReturnNone_WhenEntitiesAreNull()
        {
            // Act
            var result = EntityCompare.Compare<NamedEntity>(null, null);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.None, result);
        }

        [Fact]
        public void Compare_ShouldReturnPresentInRightOnly_WhenLeftEntityIsNull()
        {
            // Arrange
            var rightEntity = new NamedEntity { Name = "TestEntity" };

            // Act
            var result = EntityCompare.Compare<NamedEntity>(null, rightEntity);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.PresentInRightOnly, result);
        }

        [Fact]
        public void Compare_ShouldReturnPresentInLeftOnly_WhenRightEntityIsNull()
        {
            // Arrange
            var leftEntity = new NamedEntity { Name = "TestEntity" };

            // Act
            var result = EntityCompare.Compare<NamedEntity>(leftEntity, null);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.PresentInLeftOnly, result);
        }

        [Fact]
        public void Compare_ShouldReturnNone_WhenEntitiesAreEqual()
        {
            // Arrange
            var entity1 = new NamedEntity { Name = "TestEntity" };
            var entity2 = new NamedEntity { Name = "TestEntity" };

            // Act
            var result = EntityCompare.Compare<NamedEntity>(entity1, entity2);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.None, result);
        }

        [Fact]
        public void Compare_ShouldReturnChanged_WhenEntitiesAreDifferent()
        {
            // Arrange
            var entity1 = new NamedEntity { Name = "Entity1" };
            var entity2 = new NamedEntity { Name = "Entity2" };

            // Act
            var result = EntityCompare.Compare<NamedEntity>(entity1, entity2);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.Changed, result);
        }

        [Fact]
        public void Compare_ShouldReturnChanged_WhenEntityWasMutatedAfterHashWasRead()
        {
            // Arrange
            var sourceEntity = new NamedEntity { Name = "Entity", Description = "Initial" };
            _ = sourceEntity.Hash;
            sourceEntity.Description = "Updated";

            var targetEntity = new NamedEntity { Name = "Entity", Description = "Initial" };

            // Act
            var result = EntityCompare.Compare<NamedEntity>(sourceEntity, targetEntity);

            // Assert
            Assert.Equal(EntityCompare.CompareResult.Changed, result);
        }

        #endregion

        #region Compare - Sammlungen

        [Fact]
        public void Compare_ShouldReturnEmptyResult_WhenBothCollectionsAreNull()
        {
            // Act
            var result = EntityCompare.Compare<EntityCollection<NamedEntity>, NamedEntity>(null, null);

            // Assert
            Assert.Empty(result.ItemsOnlyInRight);
            Assert.Empty(result.ItemsOnlyInLeft);
            Assert.Empty(result.ChangedItems);
            Assert.Empty(result.UnchangedItems);
        }

        [Fact]
        public void Compare_ShouldReturnAllRight_WhenLeftCollectionIsNull()
        {
            // Arrange
            var rightCollection = new EntityCollection<NamedEntity>
            {
                new("Entity1"),
                new("Entity2")
            };

            // Act
            var result = EntityCompare.Compare<EntityCollection<NamedEntity>, NamedEntity>(null, rightCollection);

            // Assert
            Assert.Equal(2, result.ItemsOnlyInRight.Count);
            Assert.Contains(result.ItemsOnlyInRight, x => x.Right?.Name == "Entity1");
            Assert.Contains(result.ItemsOnlyInRight, x => x.Right?.Name == "Entity2");
        }

        [Fact]
        public void Compare_ShouldReturnAllLeft_WhenRightCollectionIsNull()
        {
            // Arrange
            var leftCollection = new EntityCollection<NamedEntity>
            {
                new NamedEntity { Name = "Entity1" },
                new NamedEntity { Name = "Entity2" }
            };

            // Act
            var result = EntityCompare.Compare<EntityCollection<NamedEntity>, NamedEntity>(leftCollection, null);

            // Assert
            Assert.Equal(2, result.ItemsOnlyInLeft.Count);
            Assert.Contains(result.ItemsOnlyInLeft, x => x.Left?.Name == "Entity1");
            Assert.Contains(result.ItemsOnlyInLeft, x => x.Left?.Name == "Entity2");
        }

        [Fact]
        public void Compare_ShouldDetectUnchangedEntities()
        {
            // Arrange
            var leftCollection = new EntityCollection<NamedEntity>
            {
                new NamedEntity { Name = "Entity1" },
                new NamedEntity { Name = "Entity2" }
            };

            var rightCollection = new EntityCollection<NamedEntity>
            {
                new NamedEntity { Name = "Entity1" },
                new NamedEntity { Name = "Entity2" }
            };

            // Act
            var result = EntityCompare.Compare<EntityCollection<NamedEntity>, NamedEntity>(leftCollection, rightCollection);

            // Assert
            Assert.Equal(2, result.UnchangedItems.Count);
        }

        [Fact]
        public void Compare_ShouldDetectChangedEntities()
        {
            // Arrange
            var leftCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity1",4711)
            };

            var rightCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity1_Modified",4711)
            };

            // Act
            var result = EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection);

            // Assert
            Assert.Single(result.ChangedItems);
            Assert.Equal("Entity1", result.ChangedItems[0].Left?.Name);
            Assert.Equal("Entity1_Modified", result.ChangedItems[0].Right?.Name);
        }


        [Fact]
        public void Compare_ShouldThrowException_OnUidCollision()
        {
            // Arrange
            var leftCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity1",123)
            };

            var rightCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity1",123),
                new("Entity2",123)
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection)
            );

            Assert.Contains("Collision in unique id found", exception.Message);
        }

        [Fact]
        public void Compare_ShouldThrowException_OnUidCollision_LeftSide()
        {
            // Arrange
            var leftCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity1", 123),
                new("Entity2", 123) // Kollision: gleiche UniqueId auf der linken Seite
            };

            var rightCollection = new EntityCollection<UidTestEntity>
            {
                new("Entity3", 999)
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection)
            );

            Assert.Contains("Collision in unique id found on the left side", exception.Message);
        }

        #endregion

        #region ItemsOnlyInRight (Deleted)

        [Theory]
        [InlineData("Entity1", 123)]
        [InlineData("Entity2", 456)]
        public void Compare_ShouldDetectDeletedItems(string name, long uniqueId)
        {
            // Arrange
            var leftCollection = CreateCollection(); // Leer
            var rightCollection = CreateCollection((name, uniqueId));

            // Act
            var result = EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection);

            // Assert
            Assert.Single(result.ItemsOnlyInRight);
            Assert.Contains(result.ItemsOnlyInRight, x => x.Right?.Name == name && x.Right?.UniqueId == uniqueId);
        }

        #endregion

        #region ItemsOnlyInLeft (Added)

        [Theory]
        [InlineData("Entity3", 789)]
        [InlineData("Entity4", 1011)]
        public void Compare_ShouldDetectAddedItems(string name, long uniqueId)
        {
            // Arrange
            var leftCollection = CreateCollection((name, uniqueId));
            var rightCollection = CreateCollection(); // Leer

            // Act
            var result = EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection);

            // Assert
            Assert.Single(result.ItemsOnlyInLeft);
            Assert.Contains(result.ItemsOnlyInLeft, x => x.Left?.Name == name && x.Left?.UniqueId == uniqueId);
        }

        #endregion

        #region Changed & Unchanged mit DynamicProperties

        public static IEnumerable<object[]> GetDynamicPropertyTestData()
        {
            yield return new object[] { "Entity1", 123, "PropertyA", 42, 42, true }; // Unchanged
            yield return new object[] { "Entity2", 456, "PropertyB", 100, 200, false }; // Changed
        }

        [Theory]
        [MemberData(nameof(GetDynamicPropertyTestData))]
        public void Compare_ShouldDetectChangedAndUnchangedProperties(
            string name, long uniqueId, string propertyName, int leftValue, int rightValue, bool isUnchanged)
        {
            // Arrange
            var leftEntity = new UidTestEntity(name, uniqueId);
            leftEntity.SetDynamicProperty(propertyName, leftValue);

            var rightEntity = new UidTestEntity(name, uniqueId);
            rightEntity.SetDynamicProperty(propertyName, rightValue);

            var leftCollection = new EntityCollection<UidTestEntity> { leftEntity };
            var rightCollection = new EntityCollection<UidTestEntity> { rightEntity };

            // Act
            var result = EntityCompare.Compare<EntityCollection<UidTestEntity>, UidTestEntity>(leftCollection, rightCollection);

            // Assert
            if (isUnchanged)
            {
                Assert.Single(result.UnchangedItems);
                Assert.Equal(leftValue, result.UnchangedItems[0].Left?.GetDynamicProperty<int>(propertyName));
            }
            else
            {
                Assert.Single(result.ChangedItems);
                Assert.Equal(leftValue, result.ChangedItems[0].Left?.GetDynamicProperty<int>(propertyName));
                Assert.Equal(rightValue, result.ChangedItems[0].Right?.GetDynamicProperty<int>(propertyName));
            }
        }

        #endregion
    }
}
