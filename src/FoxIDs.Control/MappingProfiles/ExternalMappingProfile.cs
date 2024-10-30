using AutoMapper;
using FoxIDs.Models;
using ExtInv = FoxIDs.Models.ExternalInvoice;

namespace FoxIDs.MappingProfiles
{
    public class ExternalMappingProfile : Profile
    {
        public ExternalMappingProfile()
        {
            Mapping();
        }

        private void Mapping()
        {
            CreateMap<Invoice, ExtInv.InvoiceRequest>();
            CreateMap<InvoiceLine, ExtInv.InvoiceLine>();
            CreateMap<UsedItem, ExtInv.UsedItem>();
            CreateMap<Customer, ExtInv.Customer>();
        }
    }
}
