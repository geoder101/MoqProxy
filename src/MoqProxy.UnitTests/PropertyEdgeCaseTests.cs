// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class PropertyEdgeCaseTests
{
    [Fact]
    public void SetupAsProxy_ReadOnlyProperty_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new ReadOnlyPropertyImpl();
        impl.SetValue(123);
        var mock = new Mock<IReadOnlyProperty>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value = mock.Object.ReadOnlyProp;

        /* Assert */

        Assert.Equal(123, value);
    }

    [Fact]
    public void SetupAsProxy_WriteOnlyProperty_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new WriteOnlyPropertyImpl();
        var mock = new Mock<IWriteOnlyProperty>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.WriteOnlyProp = 456;

        /* Assert */

        Assert.Equal(456, impl.InternalValue);
    }

    [Fact]
    public void SetupAsProxy_MultipleProperties_ShouldForwardAllGets()
    {
        /* Arrange */

        var impl = new MultiplePropertiesImpl
        {
            Prop1 = 1,
            Prop2 = "test",
            Prop3 = true,
            Prop4 = 3.14
        };
        var mock = new Mock<IMultipleProperties>();
        mock.SetupAsProxy(impl);

        /* Act & Assert */

        Assert.Equal(1, mock.Object.Prop1);
        Assert.Equal("test", mock.Object.Prop2);
        Assert.True(mock.Object.Prop3);
        Assert.Equal(3.14, mock.Object.Prop4);
    }

    [Fact]
    public void SetupAsProxy_MultipleProperties_ShouldForwardAllSets()
    {
        /* Arrange */

        var impl = new MultiplePropertiesImpl();
        var mock = new Mock<IMultipleProperties>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Prop1 = 10;
        mock.Object.Prop2 = "hello";
        mock.Object.Prop3 = false;
        mock.Object.Prop4 = 2.71;

        /* Assert */

        Assert.Equal(10, impl.Prop1);
        Assert.Equal("hello", impl.Prop2);
        Assert.False(impl.Prop3);
        Assert.Equal(2.71, impl.Prop4);
    }

    [Fact]
    public void SetupAsProxy_ListProperty_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new PropertyWithComplexTypeImpl();
        impl.ListProp.Add("item1");
        impl.ListProp.Add("item2");
        var mock = new Mock<IPropertyWithComplexType>();
        mock.SetupAsProxy(impl);

        /* Act */

        var list = mock.Object.ListProp;

        /* Assert */

        Assert.Equal(2, list.Count);
        Assert.Equal("item1", list[0]);
        Assert.Equal("item2", list[1]);
    }

    [Fact]
    public void SetupAsProxy_ListProperty_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new PropertyWithComplexTypeImpl();
        var mock = new Mock<IPropertyWithComplexType>();
        mock.SetupAsProxy(impl);
        var newList = new List<string> { "a", "b", "c" };

        /* Act */

        mock.Object.ListProp = newList;

        /* Assert */

        Assert.Same(newList, impl.ListProp);
        Assert.Equal(3, impl.ListProp.Count);
    }

    [Fact]
    public void SetupAsProxy_DictionaryProperty_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new PropertyWithComplexTypeImpl();
        impl.DictProp[1] = "one";
        impl.DictProp[2] = "two";
        var mock = new Mock<IPropertyWithComplexType>();
        mock.SetupAsProxy(impl);

        /* Act */

        var dict = mock.Object.DictProp;

        /* Assert */

        Assert.Equal(2, dict.Count);
        Assert.Equal("one", dict[1]);
        Assert.Equal("two", dict[2]);
    }

    [Fact]
    public void SetupAsProxy_DictionaryProperty_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new PropertyWithComplexTypeImpl();
        var mock = new Mock<IPropertyWithComplexType>();
        mock.SetupAsProxy(impl);
        var newDict = new Dictionary<int, string> { { 10, "ten" }, { 20, "twenty" } };

        /* Act */

        mock.Object.DictProp = newDict;

        /* Assert */

        Assert.Same(newDict, impl.DictProp);
        Assert.Equal(2, impl.DictProp.Count);
    }

    [Fact]
    public void SetupAsProxy_PropertyModificationsThroughMock_ShouldReflectInImplementation()
    {
        /* Arrange */

        var impl = new PropertyWithComplexTypeImpl();
        var mock = new Mock<IPropertyWithComplexType>();
        mock.SetupAsProxy(impl);

        /* Act */

        var list = mock.Object.ListProp;
        list.Add("item");

        /* Assert */

        Assert.Single(impl.ListProp);
        Assert.Equal("item", impl.ListProp[0]);
        Assert.Single(mock.Object.ListProp);
    }

    [Fact]
    public void SetupAsProxy_NullPropertyValue_ShouldForward()
    {
        /* Arrange */

        var impl = new MultiplePropertiesImpl { Prop2 = null! };
        var mock = new Mock<IMultipleProperties>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value = mock.Object.Prop2;

        /* Assert */

        Assert.Null(value);
    }

    [Fact]
    public void SetupAsProxy_SettingPropertyToNull_ShouldForward()
    {
        /* Arrange */

        var impl = new MultiplePropertiesImpl { Prop2 = "initial" };
        var mock = new Mock<IMultipleProperties>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Prop2 = null!;

        /* Assert */

        Assert.Null(impl.Prop2);
        Assert.Null(mock.Object.Prop2);
    }
}