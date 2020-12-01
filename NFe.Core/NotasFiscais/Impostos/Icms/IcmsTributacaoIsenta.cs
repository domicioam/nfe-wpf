﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    public class IcmsTributacaoIsenta : Icms
    {
        public IcmsTributacaoIsenta(OrigemMercadoria origem, decimal aliquota) : base(CstEnum.CST40, origem, aliquota)
        {
        }
    }
}