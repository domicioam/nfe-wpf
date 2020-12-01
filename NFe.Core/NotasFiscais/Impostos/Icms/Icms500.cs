﻿using NFe.Core.NotasFiscais.Impostos.Icms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    /// <summary>
    /// Equivalente do ICMS 60 no Simples Nacional
    /// </summary>
    public class Icms500 : IcmsSubstituicaoTributariaRetidoAnteiormente, FundoCombatePobreza
    {
        public Icms500(CstEnum cst, OrigemMercadoria origem) : base(cst, origem)
        {
        }

        public NotasFiscais.FundoCombatePobreza FundoCombatePobreza => throw new NotImplementedException();
    }
}