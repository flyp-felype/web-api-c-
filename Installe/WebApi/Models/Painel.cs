using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADM.ECO.API.Models
{
    public class Painel
    {
        /// <summary>Brazilian social security number</summary>
        public Decimal? CGCCPF { get; set; }
        /// <summary>Client Name</summary>
        public String NOMCLI { get; set; }
        /// <summary>Postal code</summary>
        public Decimal? CEP { get; set; }
        /// <summary>Address Number</summary>
        public Decimal? NUMERO { get; set; }
    }

    public class Client_model
    {
        private String server;
        private String database;
        private Int32? commandTimeout;

        public Client_model(String server, String database, Int32? commandTimeout)
        {
            this.server = server;
            this.database = database;
            this.commandTimeout = commandTimeout;
        }

        public Painel GetItem(Decimal CPF)
        {
            db db = new db(server, database, commandTimeout);
            String sql = @"
                            select
	                            cliente.NOMCLI,
	                            cliente.CGCCPF,
	                            endcli.CEP,
	                            endcli.NUMERO
                            from
	                            CAD_CLIENTE					cliente	(nolock)
		                            inner join CAD_ENDCLI	endcli	(nolock)
			                            on	cliente.CODCLI	=	endcli.CODCLI
                            where
		                            cliente.CGCCPF	=	'" + CPF.ToString() + @"'
	                            and	cliente.STATUS	<>	9";
            List<Painel> client = db.Select<Painel>(sql);

            return client.Count > 0 ? client.LastOrDefault() : null;
        }
    }
}