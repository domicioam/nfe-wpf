﻿using NFe.Core.Cadastro.Certificado;
using NFe.Core.Domain;
using NFe.Core.NFeInutilizacao4;
using NFe.Core.NotasFiscais;
using NFe.Core.NotasFiscais.Services;
using NFe.Core.Utils.Assinatura;
using NFe.Core.Utils.Conversores;
using System;
using Envio = NFe.Core.XmlSchemas.NfeInutilizacao2.Envio;
using Retorno = NFe.Core.XmlSchemas.NfeInutilizacao2.Retorno;
using Status = NFe.Core.NotasFiscais.Sefaz.NfeInutilizacao2.Status;

namespace NFe.Core.Sefaz.Facades
{
    public class InutilizarNotaFiscalFacade : IInutilizarNotaFiscalService
    {
        private INotaInutilizadaService _notaInutilizadaService;
        private SefazSettings _sefazSettings;
        private ICertificadoService _certificadoService;
        private IServiceFactory _serviceFactory;

        public virtual MensagemRetornoInutilizacao InutilizarNotaFiscal(string ufEmitente, CodigoUfIbge codigoUf, string cnpjEmitente, Modelo modeloNota,
            string serie, string numeroInicial, string numeroFinal)
        {
            // Pegar ambiente no App.Config
            var mensagemRetorno = InutilizarNotaFiscal(ufEmitente, codigoUf, _sefazSettings.Ambiente, cnpjEmitente,
                modeloNota,
                serie, numeroInicial, numeroFinal);

            if (mensagemRetorno.Status != Status.ERRO)
            {
                var notaInutilizada = new NotaInutilizadaTO
                {
                    DataInutilizacao = DateTime.Now,
                    IdInutilizacao = mensagemRetorno.IdInutilizacao,
                    Modelo = modeloNota == Modelo.Modelo55 ? 55 : 65,
                    Motivo = mensagemRetorno.MotivoInutilizacao,
                    Numero = numeroInicial,
                    Protocolo = mensagemRetorno.ProtocoloInutilizacao,
                    Serie = serie
                };

                _notaInutilizadaService.Salvar(notaInutilizada, mensagemRetorno.Xml);
            }

            return mensagemRetorno;
        }

        public InutilizarNotaFiscalFacade(INotaInutilizadaService notaInutilizadaService, SefazSettings sefazSettings, ICertificadoService certificadoService, IServiceFactory serviceFactory)
        {
            _notaInutilizadaService = notaInutilizadaService;
            _sefazSettings = sefazSettings;
            _certificadoService = certificadoService;
            _serviceFactory = serviceFactory;
        }

        protected InutilizarNotaFiscalFacade()
        {

        }

        private MensagemRetornoInutilizacao InutilizarNotaFiscal(string ufEmitente, CodigoUfIbge codigoUf, Ambiente ambiente, string cnpjEmitente, Modelo modeloNota,
            string serie, string numeroInicial, string numeroFinal)
        {
            var inutNFe = new Envio.TInutNFe();
            inutNFe.versao = "4.00";

            var infInut = new Envio.TInutNFeInfInut();
            infInut.tpAmb = (Envio.TAmb)(int)ambiente;
            infInut.xServ = Envio.TInutNFeInfInutXServ.INUTILIZAR;
            infInut.cUF = (Envio.TCodUfIBGE)(int)UfToTCOrgaoIBGEConversor.GetTCOrgaoIBGE(ufEmitente);
            infInut.ano = DateTime.Now.ToString("yy");
            infInut.CNPJ = cnpjEmitente;
            infInut.mod = (Envio.TMod)(int)modeloNota;
            infInut.serie = serie;
            infInut.nNFIni = numeroInicial;
            infInut.nNFFin = numeroFinal;
            infInut.xJust = "Não usada, quebra de sequência.";

            var cUF = infInut.cUF.ToString().Replace("Item", string.Empty);
            var modelo = modeloNota.ToString().Replace("Modelo", string.Empty);

            infInut.Id = "ID" + cUF + infInut.ano + cnpjEmitente + modelo + int.Parse(serie).ToString("D3") + int.Parse(numeroInicial).ToString("D9") + int.Parse(numeroFinal).ToString("D9");

            inutNFe.infInut = infInut;
            var xml = XmlUtil.Serialize(inutNFe, "http://www.portalfiscal.inf.br/nfe");
            var certificado = _certificadoService.GetX509Certificate2();
            var node = AssinaturaDigital.AssinarInutilizacao(xml, "#" + infInut.Id, certificado);

            var servico = _serviceFactory.GetService(modeloNota, Servico.INUTILIZACAO, codigoUf, certificado);

            var client = (NFeInutilizacao4SoapClient)servico.SoapClient;

            var result = client.nfeInutilizacaoNF(node);

            var retorno = (Retorno.TRetInutNFe)XmlUtil.Deserialize<Retorno.TRetInutNFe>(result.OuterXml);

            if (retorno.infInut.cStat.Equals("102"))
            {
                var procInut = new Retorno.ProcInut.TProcInutNFe();

                var procSerialized = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ProcInutNFe versao=\"4.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\">"
                    + node.OuterXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty)
                    + result.OuterXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty)
                    + "</ProcInutNFe>";

                return new MensagemRetornoInutilizacao()
                {
                    IdInutilizacao = infInut.Id,
                    Status = Status.SUCESSO,
                    Mensagem = retorno.infInut.xMotivo,
                    DataInutilizacao = retorno.infInut.dhRecbto,
                    MotivoInutilizacao = infInut.xJust,
                    ProtocoloInutilizacao = retorno.infInut.nProt,
                    Xml = procSerialized
                };
            }
            else
            {
                return new MensagemRetornoInutilizacao()
                {
                    Status = Status.ERRO,
                    Mensagem = retorno.infInut.xMotivo
                };
            }
        }
    }
}
