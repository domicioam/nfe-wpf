﻿using AutoFixture;
using AutoMapper;
using DgSystems.NFe.Services.Actors;
using NFe.Core;
using NFe.Core.Cadastro.Certificado;
using NFe.Core.Domain;
using NFe.Core.Entitities;
using NFe.Core.Interfaces;
using NFe.Core.NotasFiscais;
using NFe.Core.NotasFiscais.Sefaz.NfeAutorizacao;
using NFe.Core.NotasFiscais.Sefaz.NfeConsulta2;
using NFe.Core.Sefaz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DgSystems.NFe.Core.UnitTests.Services.Actors
{
    public class EmiteNFeContingenciaActorMock : EmiteNFeContingenciaActor
    {
        public enum ResultadoEsperado
        {
            CodigoStatus100,
            Duplicidade,
            Erro,
            CorrigeDuplicidade
        }

        public static int PreencheDadosNotaFiscalAposEnvioCount { get; set; }
        public static int TentaCorrigirNotaDuplicadaCount { get; private set; }

        private readonly INotaFiscalRepository notaFiscalRepository;
        private readonly IEmitenteRepository emissorService;
        private readonly IConsultarNotaFiscalService nfeConsulta;
        private readonly IServiceFactory serviceFactory;
        private readonly CertificadoService certificadoService;
        private readonly SefazSettings sefazSettings;
        private readonly TipoMensagem tipoMensagem;
        private readonly ResultadoEsperado resultadoEsperado;

        public EmiteNFeContingenciaActorMock(
            INotaFiscalRepository notaFiscalRepository,
            IEmitenteRepository emissorService,
            IConsultarNotaFiscalService nfeConsulta,
            IServiceFactory serviceFactory,
            CertificadoService certificadoService,
            SefazSettings sefazSettings,
            TipoMensagem erroValidacao,
            ResultadoEsperado resultadoEsperado,
            IMapper mapper)
            : base(notaFiscalRepository, emissorService, nfeConsulta, serviceFactory, certificadoService, sefazSettings, mapper)
        {
            this.notaFiscalRepository = notaFiscalRepository;
            this.emissorService = emissorService;
            this.nfeConsulta = nfeConsulta;
            this.serviceFactory = serviceFactory;
            this.certificadoService = certificadoService;
            this.sefazSettings = sefazSettings;
            this.tipoMensagem = erroValidacao;
            this.resultadoEsperado = resultadoEsperado;

            PreencheDadosNotaFiscalAposEnvioCount = 0;
            TentaCorrigirNotaDuplicadaCount = 0;
        }

        protected override MensagemRetornoTransmissaoNotasContingencia TransmitirLoteNotasFiscaisContingencia(List<string> nfeList, Modelo modelo)
        {
            if (tipoMensagem != TipoMensagem.Sucesso)
                return new MensagemRetornoTransmissaoNotasContingencia { TipoMensagem = tipoMensagem };
            else
                return new MensagemRetornoTransmissaoNotasContingencia
                {
                    TipoMensagem = tipoMensagem,
                    RetEnviNFeInfRec = new global::NFe.Core.XmlSchemas.NfeAutorizacao.Retorno.TRetEnviNFeInfRec { tMed = "3" }
                };
        }

        protected override List<RetornoNotaFiscal> ConsultarReciboLoteContingencia(string nRec, Modelo modelo)
        {
            if (resultadoEsperado == ResultadoEsperado.Erro)
            {
                var fixture = new Fixture();
                var retorno = fixture.Build<RetornoNotaFiscal>().Create();

                return new List<RetornoNotaFiscal>
                {
                    retorno,
                    retorno
                };
            }
            else if (resultadoEsperado == ResultadoEsperado.Duplicidade || resultadoEsperado == ResultadoEsperado.CorrigeDuplicidade)
            {
                var fixture = new Fixture();
                var retorno = fixture.Build<RetornoNotaFiscal>().Create();
                retorno.Motivo = "Duplicidade";

                return new List<RetornoNotaFiscal>
                {
                    retorno,
                    retorno
                };
            }
            else
            {
                var fixture = new Fixture();
                var retorno = fixture.Build<RetornoNotaFiscal>().Create();
                retorno.CodigoStatus = "100";
                retorno.DataAutorizacao = "2021-07-05T18:30:42-03:00";
                // retorno.Xml = ???

                return new List<RetornoNotaFiscal>
                {
                    retorno,
                    retorno
                };
            }
        }

        protected override Task<(NotaFiscalEntity, string)> PreencheDadosNotaFiscalAposEnvio(RetornoNotaFiscal resultado, NotaFiscalEntity nota)
        {
            PreencheDadosNotaFiscalAposEnvioCount++;
            return base.PreencheDadosNotaFiscalAposEnvio(resultado, nota);
        }

        protected override Task TentaCorrigirNotaDuplicada(List<string> erros, RetornoNotaFiscal resultado, NotaFiscalEntity nota)
        {
            if (resultadoEsperado == ResultadoEsperado.CorrigeDuplicidade)
                TentaCorrigirNotaDuplicadaCount++;
            return base.TentaCorrigirNotaDuplicada(erros, resultado, nota);
        }

        protected override Task<string> LoadXmlAsync(NotaFiscalEntity nota, Protocolo retornoConsulta)
        {
            return Task.FromResult("");
        }
    }
}
