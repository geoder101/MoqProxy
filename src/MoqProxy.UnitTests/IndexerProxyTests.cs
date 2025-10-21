// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class IndexerProxyTests
{
    [Fact]
    public void SetupAsProxy_IndexerSingleParam_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new IndexerSingleParamImpl();
        impl[5] = 42;
        var mock = new Mock<IIndexerSingleParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value = mock.Object[5];

        /* Assert */

        Assert.Equal(42, value);
    }

    [Fact(Skip = "Known Issue: Moq does not support write-only indexers directly.")]
    public void SetupAsProxy_IndexerSingleParam_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new IndexerSingleParamImpl();
        var mock = new Mock<IIndexerSingleParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object[5] = 99;

        /* Assert */

        Assert.Equal(99, impl[5]);
        Assert.Equal(99, mock.Object[5]);
    }

    [Fact]
    public void SetupAsProxy_IndexerMultiParam_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new IndexerMultiParamImpl();
        impl[1, 2] = "test";
        var mock = new Mock<IIndexerMultiParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value = mock.Object[1, 2];

        /* Assert */

        Assert.Equal("test", value);
    }

    [Fact(Skip = "Known Issue: Moq does not support write-only indexers directly.")]
    public void SetupAsProxy_IndexerMultiParam_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new IndexerMultiParamImpl();
        var mock = new Mock<IIndexerMultiParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object[1, 2] = "hello";

        /* Assert */

        Assert.Equal("hello", impl[1, 2]);
        Assert.Equal("hello", mock.Object[1, 2]);
    }

    [Fact]
    public void SetupAsProxy_IndexerReadOnly_ShouldForwardGet()
    {
        /* Arrange */

        var impl = new IndexerReadOnlyImpl();
        var mock = new Mock<IIndexerReadOnly>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value1 = mock.Object["key1"];
        var value2 = mock.Object["key2"];
        var value3 = mock.Object["nonexistent"];

        /* Assert */

        Assert.Equal(100, value1);
        Assert.Equal(200, value2);
        Assert.Equal(-1, value3);
    }

    [Fact(Skip = "Known Issue: Moq does not support write-only indexers directly.")]
    public void SetupAsProxy_IndexerWriteOnly_ShouldForwardSet()
    {
        /* Arrange */

        var impl = new IndexerWriteOnlyImpl();
        var mock = new Mock<IIndexerWriteOnly>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object["key1"] = 555;

        /* Assert */

        Assert.Equal(555, impl.Data["key1"]);
    }

    [Fact(Skip = "Known Issue: Moq does not support multi-parameter indexers well.")]
    public void SetupAsProxy_IndexerSingleParam_MultipleIndexes()
    {
        /* Arrange */

        var impl = new IndexerSingleParamImpl();
        var mock = new Mock<IIndexerSingleParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object[0] = 10;
        mock.Object[1] = 20;
        mock.Object[2] = 30;

        /* Assert */

        Assert.Equal(10, mock.Object[0]);
        Assert.Equal(20, mock.Object[1]);
        Assert.Equal(30, mock.Object[2]);
        Assert.Equal(10, impl[0]);
        Assert.Equal(20, impl[1]);
        Assert.Equal(30, impl[2]);
    }

    [Fact(Skip = "Known issue with multi-parameter indexers in Moq proxying")]
    public void SetupAsProxy_IndexerMultiParam_MultipleIndexes()
    {
        /* Arrange */

        var impl = new IndexerMultiParamImpl();
        var mock = new Mock<IIndexerMultiParam>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object[0, 0] = "a";
        mock.Object[0, 1] = "b";
        mock.Object[1, 0] = "c";

        /* Assert */

        Assert.Equal("a", mock.Object[0, 0]);
        Assert.Equal("b", mock.Object[0, 1]);
        Assert.Equal("c", mock.Object[1, 0]);
        Assert.Equal("a", impl[0, 0]);
        Assert.Equal("b", impl[0, 1]);
        Assert.Equal("c", impl[1, 0]);
    }
}