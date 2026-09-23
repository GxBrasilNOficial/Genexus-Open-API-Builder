# B128 — contrato versionado para referências C#

Data: 2026-09-23. Este documento é a especificação versionada do comportamento entregue pelo B128. O manuscrito v8 em `Temp/revisao-por-pares/B128-20260923-0925/manuscrito-v8.md` é proveniência local da discussão e da decisão; `Temp/*` é ignorado pelo Git e o manuscrito não é fonte normativa do repositório.

## Objetivo e alcance

O checker do pré-push reduz a entrada de novas referências a linhas C# em Markdown de `Docs/` e valida referências históricas fixas novas. O check é sintático e incremental. Ele não migra referências legadas e não confirma que a afirmação ao redor de uma citação corresponde ao comportamento do código.

## Referências móveis por dois-pontos

- O tokenizer reconhece candidatos `.cs:` e `.CS:` no Markdown fonte. A unidade numérica completa é examinada; um prefixo aparentemente válido não pode escapar de um sufixo ou continuação malformada.
- A forma aceita contém uma linha decimal positiva sem zero inicial, ou uma faixa inclusiva crescente ou unitária. Localização vazia, zero inicial, faixa incompleta, coluna e sufixo tornam o candidato inválido.
- A chave usa o caminho completo, preserva caixa e normaliza `/` e `\`. Formatação Markdown e escape documentados não alteram a chave. Formatos fora da gramática do tokenizer ficam em `notCovered`.
- O check compara multiconjuntos globais nas árvores Git `origin/main` e `HEAD`. Qualquer aumento de uma chave reconhecida falha; remoções podem compensar acréscimos da mesma chave em outro documento. Tokens legados sem aumento permanecem sem migração.

## Referências fixas por âncora

- O canal `.cs#L` reconhece candidatos de intenção permissivamente, mas valida cada ocorrência excedente com rigor. A forma válida é um SHA completo de commit, path Git relativo terminado em `.cs` e linha positiva ou faixa inclusiva crescente.
- O commit precisa ser ancestral de `HEAD`; o path não pode ser absoluto, conter `..` ou usar barras invertidas, e precisa resolver para um blob C# naquele commit. As linhas precisam existir na contagem física do blob, considerando `LF`, `CRLF` e `CR`.
- Um endereço fixo novo válido passa. Um endereço fixo novo inválido falha. Falha operacional do Git, leitura não confiável ou impossibilidade de provar ancestralidade resulta em `environmentBlocked`, não em referência inválida.
- A comparação preserva a grafia relevante à validade, incluindo caminho, prefixo e caixa. Não normaliza barras neste canal.

## Aviso de CHANGELOG e estados

- O aviso `b128-changelog-line-reference` não bloqueia. Examina apenas linhas adicionadas no diff commitado de `Docs/` que associem `CHANGELOG` a uma referência numérica provável na mesma linha ou na anterior, em português, inglês ou espanhol. Falha Git ou de decodificação produz aviso explícito de cobertura incompleta.
- Sem `origin/main`, o check de referências fica `skipped` e o pré-requisito remoto mantém seu próprio estado de bloqueio. Com commits remotos à frente, o check fica `skipped` como `notEvaluated/remoteBehind`.
- O resultado cobre apenas as gramáticas reconhecidas e os arquivos Markdown em `Docs/`. Não cobre legado deslocado sem aumento global, prosa sem padrão reconhecível, variantes arbitrárias de Markdown/HTML, arquivos fora de `Docs/`, `Arquivo.cs(linha,coluna)`, `Arquivo.cs:line N`, nem a correção semântica da afirmação citada. A heurística de CHANGELOG também pode ter falsos positivos e falsos negativos.

## Implementação e evidência

O tokenizer compartilhado, as integrações no checker, os testes, os resultados e os limites observados estão documentados em [B128 — implementação offline e gates](2026-09-23-B128-IMPLEMENTACAO-E-GATES.md). A revisão que originou esta especificação está preservada localmente no manuscrito v8 indicado acima; mudanças ao contrato exigem atualizar este documento versionado e os testes correspondentes.
