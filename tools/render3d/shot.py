import sys, base64, subprocess, time, os
from playwright.sync_api import sync_playwright
out=sys.argv[1]; os.makedirs(out,exist_ok=True)
N=int(sys.argv[2]); yaws=[float(x) for x in sys.argv[3].split(',')]; tags=sys.argv[4].split(',')
srv=subprocess.Popen([sys.executable,'-m','http.server','8765'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL); time.sleep(1)
try:
  with sync_playwright() as p:
    b=p.chromium.launch(args=['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--use-gl=angle'])
    pg=b.new_page(); pg.on('console',lambda m:print('console:',m.text)); pg.on('pageerror',lambda e:print('err:',e))
    pg.goto('http://localhost:8765/index.html'); pg.wait_for_function('window.ready===true',timeout=20000)
    for tag,yaw in zip(tags,yaws):
      for i in range(N):
        d=pg.evaluate(f'renderFrame({i/N},{yaw})')
        open(f'{out}/{tag}_{i+1:02d}.png','wb').write(base64.b64decode(d.split(',')[1]))
    b.close()
finally: srv.terminate()
