"""Auditoria local de candidatos Git; nunca imprime valores de secretos."""
import hashlib, json, pathlib, re, subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]
patterns = {
    'clave_privada': re.compile(rb'-{5}BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-{5}'),
    'json_credencial': re.compile(rb'"(?:private_key|client_secret|refresh_token)"\s*:\s*"[^"\s][^"]{8,}"'),
    'token_github': re.compile(rb'gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}'),
    'clave_google': re.compile(rb'AIza[0-9A-Za-z_-]{30,}'),
}
paths = subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'], cwd=ROOT).decode().split('\0')
findings, big, total = [], [], 0
for name in sorted(set(filter(None, paths))):
    path = ROOT / name
    if not path.is_file(): continue
    size = path.stat().st_size; total += size
    if size > 20 * 1024**2: big.append({'path': name, 'bytes': size})
    if size > 5 * 1024**2: continue  # binaries/atlases are inventoried, not treated as text
    data = path.read_bytes()
    if b'\0' in data[:4096]: continue
    for kind, pattern in patterns.items():
        for match in pattern.finditer(data):
            findings.append({'path': name, 'line': data[:match.start()].count(b'\n') + 1, 'kind': kind})
report = {'candidate_files': len(set(filter(None, paths))), 'candidate_bytes': total,
          'large_files': big, 'secret_findings': findings,
          'oversized_files': [item for item in big if item['bytes'] >= 100 * 1024**2],
          'scope': 'Candidatos Git de la raiz original; heuristica, no prueba matematica de ausencia. PreparacionGitHub excluida; Git anterior sin commits archivado externamente.'}
(ROOT / 'docs' / 'auditoria-git.json').write_text(json.dumps(report, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))
raise SystemExit(1 if findings or report['oversized_files'] else 0)
