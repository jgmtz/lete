import sys, json, time, subprocess
from playwright.sync_api import sync_playwright
N=int(sys.argv[1]); out=sys.argv[2]
srv=subprocess.Popen(['python3','-m','http.server','8766'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL); time.sleep(1)
res={}
try:
  with sync_playwright() as p:
    b=p.chromium.launch(args=['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--use-gl=angle'])
    pg=b.new_page(); pg.on('pageerror',lambda e:print('err',e))
    pg.goto('http://localhost:8766/index.html'); pg.wait_for_function('window.ready===true',timeout=20000)
    for tag,yaw in (('f',40),('c',140),('l',-90),('d',90),('f0',0),('c0',180)):
      res[tag]={'rest':pg.evaluate(f'joints(null,{yaw})'),'frames':[pg.evaluate(f'joints({i}/{N},{yaw})') for i in range(N)]}
    b.close()
finally: srv.terminate()
json.dump(res,open(out,'w'))
print('ok',list(res['f']['rest'].keys()))
