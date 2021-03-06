﻿using System.Threading.Tasks;
using Jasper.Marten;
using Marten;

namespace ShowHandler
{
    public class CreateItemHandler
    {
        private readonly IDocumentSession _session;

        public CreateItemHandler(IDocumentSession session)
        {
            _session = session;
        }

        public void Handle(CreateItemCommand command)
        {
            var item = new Item {Name = command.Name};
            _session.Store(item);
            _session.SaveChanges();
        }
    }


    /*
    public class CreateItemHandler
    {
        [MartenTransaction]
        public static ItemCreatedEvent Handle(CreateItemCommand command, IDocumentSession session)
        {
            var item = new Item {Name = command.Name};
            session.Store(item);

            return new ItemCreatedEvent{Item = item};
        }
    }*/
}
