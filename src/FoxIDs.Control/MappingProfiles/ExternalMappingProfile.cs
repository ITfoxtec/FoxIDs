using AutoMapper;
using FoxIDs.Models;
using ExtInv = FoxIDs.Models.ExternalInvoices;

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
            CreateMap<Seller, ExtInv.Seller>();
            CreateMap<Customer, ExtInv.Customer>();
        }
    }
}
