import re, sys, html
src = open(sys.argv[1], encoding='utf-8').read()
def inline(t):
    t = html.escape(t, quote=False)
    t = re.sub(r'`([^`]+)`', r'<code>\1</code>', t)
    t = re.sub(r'\*\*(.+?)\*\*', r'<strong>\1</strong>', t)
    t = re.sub(r'(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])', r'<em>\1</em>', t)
    t = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2">\1</a>', t)
    return t
out, para, lst, toc = [], [], None, []
def flush():
    global para
    if para: out.append('<p>' + inline(' '.join(para)) + '</p>'); para = []
def close_list():
    global lst
    if lst: out.append('</%s>' % lst); lst = None
for line in src.split('\n'):
    m = re.match(r'^(#{1,3}) (.*)', line)
    if m:
        flush(); close_list(); lvl = len(m.group(1)); txt = m.group(2)
        if lvl == 1: out.append('<h1>%s</h1>' % inline(txt)); continue
        sid = re.sub(r'[^a-z0-9]+', '-', txt.lower().encode('ascii', 'ignore').decode()).strip('-') or 'sec'
        if lvl == 2: toc.append((sid, txt))
        out.append('<h%d id="%s">%s</h%d>' % (lvl, sid, inline(txt), lvl)); continue
    if line.strip() == '---': flush(); close_list(); out.append('<hr>'); continue
    m = re.match(r'^(\s*)([-*]|\d+\.) (.*)', line)
    if m:
        flush(); kind = 'ol' if m.group(2)[0].isdigit() else 'ul'
        if lst != kind: close_list(); out.append('<%s>' % kind); lst = kind
        out.append('<li>' + inline(m.group(3)) + '</li>'); continue
    if line.strip() == '': flush(); close_list(); continue
    if lst and line.startswith('   '): out[-1] = out[-1][:-5] + ' ' + inline(line.strip()) + '</li>'; continue
    para.append(line.strip())
flush(); close_list()
body = '\n'.join(out)
nav = '\n'.join('<a href="#%s">%s</a>' % (sid, html.escape(t)) for sid, t in toc)
print(open(sys.argv[2], encoding='utf-8').read().replace('{{BODY}}', body).replace('{{TOC}}', nav))
