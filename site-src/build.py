# Génère site/conseils/*.html à partir de site-src/conseils/*.md (en-tête YAML minimal : title, desc, image, date).
import os, re, sys, html, subprocess, json
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.dirname(here)
tpl = open(os.path.join(here, 'article-template.html'), encoding='utf-8').read()
pages = []
for name in sorted(os.listdir(os.path.join(here, 'conseils'))):
    if not name.endswith('.md'): continue
    src = open(os.path.join(here, 'conseils', name), encoding='utf-8').read()
    m = re.match(r'^---\n(.*?)\n---\n(.*)$', src, re.S)
    meta = dict(re.findall(r'^(\w+):\s*(.*)$', m.group(1), re.M))
    body_md = m.group(2)
    slug = name[:-3]
    path = 'conseils/' + slug + '.html'
    tmp_md = os.path.join(here, '.tmp.md'); open(tmp_md, 'w', encoding='utf-8').write(body_md)
    tmp_tpl = os.path.join(here, '.tmp.html'); open(tmp_tpl, 'w', encoding='utf-8').write('{{TOC}}\n<<<SPLIT>>>\n{{BODY}}')
    out = subprocess.check_output([sys.executable, os.path.join(here, 'md2html.py'), tmp_md, tmp_tpl]).decode('utf-8')
    toc, body = out.split('<<<SPLIT>>>\n', 1)
    extra = ''
    if meta.get('faq'):
        qa = re.findall(r'<h3 id="[^"]*">(.*?)</h3>\n<p>(.*?)</p>', body, re.S)
        ents = [{"@type": "Question", "name": re.sub('<[^>]+>', '', q), "acceptedAnswer": {"@type": "Answer", "text": re.sub('<[^>]+>', '', a)}} for q, a in qa]
        extra = ',\n    ' + json.dumps({"@type": "FAQPage", "mainEntity": ents}, ensure_ascii=False)
    page = (tpl.replace('{{TITLE}}', html.escape(meta['title'], quote=False).replace('"','&quot;')).replace('{{DESC}}', html.escape(meta['desc'], quote=False).replace('"','&quot;'))
            .replace('{{PATH}}', path).replace('{{IMAGE}}', meta.get('image', 'accueil-jour.jpg')).replace('{{DATE}}', meta.get('date', '2026-11-03'))
            .replace('{{TOC}}', toc.strip()).replace('{{BODY}}', '<p class="meta">Par Nicolas Kowalski, papa de Laura · ' + meta.get('date', '2026-11-03') + '</p>\n' + body.strip()).replace('{{EXTRA_LD}}', extra))
    open(os.path.join(root, 'site', path), 'w', encoding='utf-8').write(page)
    pages.append((path, meta))
    os.remove(tmp_md); os.remove(tmp_tpl)
    print('→', path)
json.dump([{'path': p, 'title': m['title'], 'desc': m['desc']} for p, m in pages], open(os.path.join(here, 'pages.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
