using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Singular.Core.Idempotency;
using Vostok.Singular.Core.Idempotency.IdempotencyControlRules;
using Vostok.Singular.Core.Idempotency.IdempotencyControlRules.Settings;

namespace Vostok.Singular.Core.Tests
{
    public class IclResolverTests
    {
        private const string POST = "POST";
        private const string fooPath = "foo";

        private IIdempotencySettingsCache<IdempotencyControlRule> iclCache;
        private IclResolver iclResolver;

        [SetUp]
        public void SetUp()
        {
            iclCache = Substitute.For<IIdempotencySettingsCache<IdempotencyControlRule>>();

            iclResolver = new IclResolver(iclCache);
        }

        [Test]
        public void Should_Be_Idempotent_When_NoRules()
        {
            var iclRulesProvider = Substitute.For<IIclRulesSettingsProvider>();
            iclRulesProvider.Get().Returns(new IdempotencySettings());
            var iclCache = new IclCache(iclRulesProvider);

            var iclResolver = new IclResolver(iclCache);

            iclResolver.IsIdempotent(POST, fooPath).Should().BeTrue();
        }

        [TestCase("*", "*")]
        [TestCase(POST, fooPath)]
        public void Should_Be_NonIdempotent_When_NonIdempotentRule(string methodPattern, string pathPattern)
        {
            iclCache.Get()
                .Returns(
                    new List<IdempotencyControlRule>(
                        new[]
                        {
                            new IdempotencyControlRule
                            {
                                Method = methodPattern,
                                IsIdempotent = false,
                                PathPattern = new Wildcard(pathPattern)
                            }
                        }));

            iclResolver.IsIdempotent(POST, fooPath).Should().BeFalse();
        }

        [TestCase("*", "*")]
        [TestCase(POST, "foo")]
        public void Should_Be_Idempotent_When_IdempotentRule_Is_First(string methodPattern, string pathPattern)
        {
            iclCache.Get()
                .Returns(
                    new List<IdempotencyControlRule>(
                    new[]
                    {
                        new IdempotencyControlRule
                        {
                            Method = methodPattern,
                            IsIdempotent = true,
                            PathPattern = new Wildcard(pathPattern)
                        },
                        new IdempotencyControlRule
                        {
                            Method = methodPattern,
                            IsIdempotent = false,
                            PathPattern = new Wildcard(pathPattern)
                        },
                    }));

            iclResolver.IsIdempotent(POST, fooPath).Should().BeTrue();
        }
    }
}