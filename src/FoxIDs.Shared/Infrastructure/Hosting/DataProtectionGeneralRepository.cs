using FoxIDs.Models.Master;
using FoxIDs.Repository;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public class DataProtectionGeneralRepository : IXmlRepository
    {
        private readonly IServiceScopeFactory factory;

        public DataProtectionGeneralRepository(IServiceScopeFactory factory)
        {
            this.factory = factory;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return GetAllElementsCore().ToList().AsReadOnly();
        }

        private IEnumerable<XElement> GetAllElementsCore()
        {
            using (var scope = factory.CreateScope())
            {
                var masterDataRepository = scope.ServiceProvider.GetRequiredService<IMasterDataRepository>();

                var dataProtections = masterDataRepository.GetListAsync<DataProtection>().GetAwaiter().GetResult();
                foreach (var item in dataProtections)
                {
                    yield return XElement.Parse(item.KeyData);
                }
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            using (var scope = factory.CreateScope())
            {
                var masterDataRepository = scope.ServiceProvider.GetRequiredService<IMasterDataRepository>();

                var item = new DataProtection
                {
                    Id = DataProtection.IdFormatAsync(Guid.NewGuid().ToString().ToLower()).GetAwaiter().GetResult(),
                    KeyData = element.ToString(SaveOptions.DisableFormatting)
                };
                masterDataRepository.CreateAsync(item).GetAwaiter().GetResult();
            }
        }
    }
}
