# Plano de Ação — Aprovação no Google AdSense (Paraki)

> **Status:** Conta recusada por descumprimento de políticas do programa AdSense.
> **Objetivo:** Identificar causas prováveis e corrigir antes de reaplicar.

---

## Diagnóstico: Causas Prováveis de Recusa

O Paraki é uma plataforma de mapa colaborativo com foco em infraestrutura de micromobilidade. Por sua natureza, é especialmente vulnerável a algumas categorias de recusa:

### 🔴 Alta Probabilidade

| Causa | Por quê se aplica ao Paraki |
|---|---|
| **Conteúdo insuficiente ("thin content")** | Plataformas de mapa têm pouco texto nativo. As páginas de detalhe de bicicletários exibem dados estruturados, mas poucas palavras originais e contextuais. |
| **Site incompleto ou em construção** | MVP recente, funcionalidades ainda em desenvolvimento — o AdSense exige um site "completo e funcional". |
| **Ausência de páginas legais obrigatórias** | Sem Política de Privacidade, Termos de Uso e Contato visíveis, o Google não aprova a conta. |

### 🟡 Probabilidade Média

| Causa | Por quê se aplica ao Paraki |
|---|---|
| **Navegação deficiente** | Se o app depende muito de JavaScript para renderizar conteúdo (Blazor WASM), o crawler do Google pode ver páginas vazias. |
| **Pouco tráfego orgânico** | O AdSense prefere sites com tráfego real e orgânico; um MVP recém-lançado raramente tem isso. |
| **Conteúdo gerado por usuários sem moderação** | Avaliações e comentários livres podem conter conteúdo impróprio; o Google exige moderação visível. |

### 🟢 Baixa Probabilidade (mas verificar)

| Causa | Por quê se aplica ao Paraki |
|---|---|
| Idioma não suportado | Português (Brasil) é suportado pelo AdSense. |
| Conta duplicada | Verificar se há conta anterior criada com o mesmo e-mail. |

---

## Plano de Ação

### Fase 1 — Pré-requisitos Legais e Estruturais (Prioridade Máxima)

Estes itens são bloqueadores absolutos — sem eles, qualquer reaprovação falhará.

- [ ] **Criar página `/politica-de-privacidade`**
  - Explicar quais dados são coletados (e-mail, localização, avaliações)
  - Mencionar uso de cookies e anúncios (AdSense exige isso explicitamente)
  - Gerar com [Privacy Policy Generator](https://www.privacypolicygenerator.info/) como base e adaptar
- [ ] **Criar página `/termos-de-uso`**
  - Responsabilidades do usuário ao adicionar dados de bicicletários
  - Política de conteúdo de avaliações (proibido conteúdo ofensivo)
- [ ] **Criar página `/contato`**
  - Formulário de contato OU e-mail público visível
  - O Google verifica se o site tem um canal de comunicação com o usuário
- [ ] **Adicionar links para essas páginas no footer em todas as telas**

---

### Fase 2 — Conteúdo (Resolver "Thin Content")

O maior risco para plataformas de mapa é a falta de texto interpretável pelo crawler.

- [ ] **Adicionar página de blog ou guias** (ex: `/guias`)
  - Artigos como "Como encontrar bicicletário seguro em [cidade]"
  - "Tipos de suporte para bicicletas: qual usar?"
  - Mínimo de 3-5 artigos com 500+ palavras cada antes de reaplicar
- [ ] **Enriquecer páginas de detalhe dos bicicletários**
  - Adicionar descrição textual automática com base nos atributos (ex: "Este bicicletário oferece tomada, vestiário e acesso livre 24h")
  - Incluir metadados SEO (`<title>`, `<meta description>`) únicos por bicicletário
- [ ] **Criar página `/sobre`**
  - Missão do Paraki, quem faz, por que existe
  - Mínimo 300 palavras

---

### Fase 3 — Rastreabilidade pelo Google (SEO Técnico)

O Blazor WASM renderiza no cliente — o Googlebot pode não ver o conteúdo.

- [ ] **Verificar se o conteúdo é indexável**
  - Testar com `https://search.google.com/test/rich-results` e `site:paraki.com.br` no Google
  - Se o crawler vê página em branco, implementar **pré-renderização** ou **SSR** (Blazor Server ou static pre-rendering)
- [ ] **Criar `sitemap.xml`**
  - Incluir todas as páginas estáticas + páginas de detalhe de bicicletários populares
  - Submeter no Google Search Console
- [ ] **Criar `robots.txt`** adequado (sem bloquear o Googlebot)
- [ ] **Verificar propriedade no Google Search Console** antes de reaplicar

---

### Fase 4 — Tráfego e Credibilidade

O AdSense considera a qualidade do tráfego do site.

- [ ] **Aguardar 4-8 semanas** de tráfego orgânico mínimo (100+ usuários/mês) após as correções
- [ ] **Divulgar o Paraki** em grupos de ciclismo, Reddit, redes sociais para gerar tráfego real
- [ ] **Configurar Google Analytics** (Universal Analytics ou GA4) para monitorar métricas antes de reaplicar
- [ ] **Verificar se não há fontes de tráfego artificiais** (bots, cliques próprios)

---

### Fase 5 — Moderação de Conteúdo UGC

O Google exige que conteúdo gerado por usuários seja moderado.

- [ ] **Implementar sistema de denúncia de avaliações** (botão "Reportar")
- [ ] **Adicionar regras claras na página de termos** sobre o que não é permitido nas avaliações
- [ ] **Moderação básica no backend**: filtrar palavrões e URLs suspeitas nos comentários

---

## Checklist Final Antes de Reaplicar

- [ ] Política de Privacidade publicada e linkada no footer
- [ ] Termos de Uso publicados e linkados no footer
- [ ] Página de Contato acessível
- [ ] Página Sobre com 300+ palavras
- [ ] Mínimo 3 artigos de blog com 500+ palavras
- [ ] Bicicletários com descrição textual automática
- [ ] Site verificado no Google Search Console
- [ ] Sitemap.xml submetido
- [ ] Conteúdo visível para o Googlebot (testar rastreamento)
- [ ] Tráfego orgânico real (mínimo 4 semanas após melhorias)
- [ ] Nenhuma conta AdSense anterior com o mesmo e-mail

---

## Ordem Recomendada de Execução

```
Semana 1-2:  Fase 1 (páginas legais) + Fase 3 (SEO técnico)
Semana 3-4:  Fase 2 (conteúdo/blog)
Semana 5-8:  Fase 4 (crescer tráfego) + Fase 5 (moderação)
Semana 9+:   Reaplicar ao AdSense
```

---

*Gerado com base nas [Políticas do Programa AdSense](https://support.google.com/adsense/answer/81904?hl=pt_BR) em 2026-06-29.*
