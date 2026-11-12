# -*- coding: utf-8 -*-
"""Compares the plugin's scan with the "Numbered books" section of the CMS check Report of TOC
(CMS\\toc_examiner\\toc_examiner.py, run under Paratext's IronPython by Project Checker's harness).

    python tests\\compare_scan.py PROJECT_DIR [PROJECT_DIR ...]
    python tests\\compare_scan.py --all "C:\\My Paratext 8 Projects"

Each project's top-level files (no .bak) are copied to a scratch folder first, because the CMS
script writes its report into the project folder. Prints one line per project and exits 1 when
any project differs: the summary line and every row of the "Numbered books" table must be identical.
"""
import io, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
SCRIPT = os.path.join(REPO, 'CMS', 'toc_examiner', 'toc_examiner.py')
HARNESS = os.path.join(REPO, 'Project_Checker', 'tests', 'harness')
TESTHOST = os.path.join(HERE, 'TestHost', 'bin', 'Release', 'NumberedBookTitlesTestHost.exe')


def copy_project(src, dst):
    os.makedirs(dst)
    # Old reports are left behind: only the one this run writes may be read.
    for fn in os.listdir(src):
        p = os.path.join(src, fn)
        if os.path.isfile(p) and not fn.lower().endswith(('.bak', '.html')):
            shutil.copy2(p, os.path.join(dst, fn))


def ps_quote(s):
    return "'" + s.replace("'", "''") + "'"


def cms_rows(project):
    out = os.path.join(os.path.dirname(project), 'cms.json')
    # -Command, not -File: with -File, "--out" would be taken as a parameter of ipy_run.ps1.
    args = [os.path.join(HARNESS, 'cms_harness.py'), '--script', SCRIPT, '--project', project,
            '--books', 'all', '--intro', 'ch1', '--out', out, '--check', 'toc']
    command = '& ' + ps_quote(os.path.join(HARNESS, 'ipy_run.ps1')) + ' ' + ' '.join(ps_quote(a) for a in args)
    p = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', command],
                       stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    if p.returncode != 0:
        sys.stderr.write(p.stderr.decode('utf-8', 'replace')[-2000:])
    path = os.path.join(project, 'TocExaminerReport.html')
    if not os.path.exists(path):
        return None
    lines = io.open(path, encoding='utf-8').read().split('\n')
    try:
        start = next(i for i, l in enumerate(lines) if l.startswith('<h2>Numbered books'))
    except StopIteration:
        return []
    rows = []
    for l in lines[start + 1:]:
        if l.startswith('<p>All numbered books') or l.startswith('</body>'):
            break
        if l.startswith('<p>') and 'numbered book(s)' in l or l.startswith('<tr><td class="bk">'):
            rows.append(l)
    return rows


def host_rows(project):
    p = subprocess.run([TESTHOST, 'scan', project], stdout=subprocess.PIPE)
    return [l for l in p.stdout.decode('utf-8').split('\r\n') if l]


def compare(src, scratch, n):
    name = os.path.basename(os.path.normpath(src))
    dst = os.path.join(scratch, '%03d' % n, name)
    copy_project(src, dst)
    a = cms_rows(dst)
    if a is None:
        print('%-12s CMS report missing' % name)
        return False
    b = host_rows(dst)
    # The CMS script prints no summary line when it has fewer than two numbered books' rows to show.
    if a == b:
        print('%-12s identical (%d rows)' % (name, max(0, len(a) - 1)))
        return True
    print('%-12s DIFFERENT' % name)
    for l in a:
        if l not in b:
            print('   cms only : ' + l)
    for l in b:
        if l not in a:
            print('   host only: ' + l)
    return False


def main(args):
    if args[:1] == ['--all']:
        root = args[1]
        projects = [os.path.join(root, d) for d in sorted(os.listdir(root))
                    if os.path.isfile(os.path.join(root, d, 'Settings.xml'))]
    else:
        projects = args
    scratch = tempfile.mkdtemp(prefix='nbt_')
    try:
        bad = [p for n, p in enumerate(projects) if not compare(p, scratch, n)]
    finally:
        shutil.rmtree(scratch, ignore_errors=True)
    print('%d project(s), %d different' % (len(projects), len(bad)))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
