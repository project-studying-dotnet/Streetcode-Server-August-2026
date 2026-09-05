using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Specifications.Partners;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.SpecificationsTests.Partners;

    public class GetPartnerByIdSpecificationTests
    {
        [Fact]
        public void Evaluate_ReturnPartnerById()
        {
            var partners = new List<Partner>
                {
                    new() { Id = 1},
                    new() { Id = 2},
                    new() { Id = 3},
                };

            var spec = new GetPartnerByIdSpecification(2);
            var result = spec.Evaluate(partners);

            var partner = Assert.Single(result);
            Assert.Equal(2, partner.Id);
        }

        [Fact]
        public void Evaluate_ReturnEmpty_WhenIdNotFound()
        {
            var partners = new List<Partner>
                {
                    new() { Id = 1},
                    new() { Id = 2},
                    new() { Id = 3},
                };

            var spec = new GetPartnerByIdSpecification(999);
            var result = spec.Evaluate(partners);

            Assert.Empty(result);
        }
    }

