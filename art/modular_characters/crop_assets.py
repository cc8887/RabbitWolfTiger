#!/usr/bin/env python3
"""Deterministic source-pixel sprite extraction. No artwork regeneration or alpha cleanup."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import json, hashlib, argparse
p=argparse.ArgumentParser(); p.add_argument('--source',default=str(Path(__file__).resolve().parent/'source/reference.png')); p.add_argument('--output',default=str(Path(__file__).resolve().parent)); a=p.parse_args()
root=Path(a.output).resolve(); src=Path(a.source).resolve(); im=Image.open(src).convert('RGBA'); root.mkdir(parents=True,exist_ok=True)
entries=[]
def add(name,section,rect,slot=None,status='component',canvas_rect=None,pivot=None,note=None):
    rect=tuple(rect); crop=im.crop(rect); canvas_rect=tuple(canvas_rect or rect)
    out=Image.new('RGBA',(canvas_rect[2]-canvas_rect[0],canvas_rect[3]-canvas_rect[1])); offset=(rect[0]-canvas_rect[0],rect[1]-canvas_rect[1]); out.paste(crop,offset)
    dest=Path(section)/(name+'.png'); (root/dest).parent.mkdir(exist_ok=True,parents=True);out.save(root/dest)
    alpha=out.getchannel('A'); meaningful=alpha.point(lambda v:255 if v>=32 else 0)
    entry={'id':name,'file':str(dest),'section':section,'status':status,'slot':slot or section,'source_rect_xyxy':rect,'source_crop_size':crop.size,'source_canvas_rect_xyxy':canvas_rect,'output_size':out.size,'source_offset_xy':offset,'alpha_bbox_xyxy':alpha.getbbox(),'alpha_ge32_bbox_xyxy':meaningful.getbbox(),'pivot_xy':pivot or [out.width/2,out.height/2],'pivot_note':'Suggested registration only; not an original rig measurement. Pixels are never trimmed.','notes':note or ''};entries.append(entry); return entry
# 1: face bases
for name,x0,x1 in [('rabbit_cream',19,132),('wolf_gray',135,247),('carrot_orange',248,361),('tiger_orange',366,479),('human_skin',482,596)]:add('face_'+name,'01_faces',(x0,67,x1,181),'face')
# 2: only the first top-row item is a true blank sclera.
e=add('sclera_blank','02_eyes',(749,61,817,130),'eye.sclera',note='Only genuinely pupil-free sclera in source. Reuse for both eyes; do not erase baked-in pupils from other eyes.');e['eye_alignment']={'center_xy':[34,34.5],'suggested_pupil_center_xy':[34,34.5],'pupil_scale':0.65,'clipping':'Clip independent pupil to sclera alpha in runtime. Suggested scale is not baked into pixels.'}
add('eye_baked_half_top','02_eyes',(830,60,901,130),'eye.composite','composite',note='White and black pupil are baked together; cannot recover underlying white without repainting.')
add('eye_baked_round_top','02_eyes',(917,60,988,130),'eye.composite','composite',note='White and black pupil are baked together.')
for name,rect in [('brown',(628,156,680,211)),('tan',(683,156,736,211)),('blue',(739,156,791,211)),('green',(795,156,847,211)),('gold',(852,156,905,211)),('slit',(914,155,940,214)),('star',(951,155,995,214))]:
 e=add('pupil_'+name,'02_eyes',((rect[0],159,rect[2],rect[3]) if name=='brown' else rect),'eye.pupil',canvas_rect=rect); e['eye_alignment']={'anchor_xy':e['pivot_xy'],'target':'sclera_blank center_xy','independent_translation':True,'note':'Colored round items include iris and black pupil as one source layer.'}
add('eye_heart_baked','02_eyes',(998,155,1055,214),'eye.composite','composite',note='Heart pupil is baked into a white disk; retained intact, not falsely labeled independent pupil.')
for name,x0,x1 in [('normal',630,681),('surprised',684,736),('half',739,790),('sleepy',793,845),('up',847,898),('down',900,951),('happy',953,1004),('closed',1007,1057)]:add('eye_shape_'+name,'02_eyes',(x0,252,x1,305),'eye.composite','composite',note='Source eye-shape preview, including its white fill and baked mark/pupil. Not a separated eyelid overlay; no hidden pixels reconstructed.')
# 3: six nose shapes in the top-right row despite the inconsistent printed headings.
for name,rect in [('triangle_pink',(1083,58,1137,110)),('triangle_black',(1153,58,1207,110)),('triangle_orange',(1226,58,1279,110)),('round_brown',(1320,60,1372,108)),('round_pink',(1389,60,1439,107)),('heart_cream',(1452,58,1509,107))]:add('nose_'+name,'03_noses',rect,'nose')
# 4: ten visible mouth candidates; five upper and five lower. Source heading says eight.
for name,rect in [('open_large',(1097,194,1167,240)),('open_medium',(1188,199,1241,241)),('open_small',(1272,199,1319,239)),('open_tiny',(1360,198,1404,229)),('line_cream',(1433,199,1507,222)),('smile',(1100,270,1159,306)),('frown',(1187,272,1249,304)),('flat',(1273,280,1330,299)),('teeth',(1359,270,1417,306)),('tongue',(1445,273,1507,309))]:add('mouth_'+name,'04_mouths',rect,'mouth')
# 5: paired items retain an identical pair canvas, with only their own source region pasted.
for name,group,left,right in [
 ('rabbit_ear',(31,395,114,469),(31,395,74,469),(76,395,114,469)),
 ('wolf_ear',(145,398,234,468),(145,398,191,468),(194,398,234,468)),
 ('tiger_ear',(259,409,360,466),(259,409,309,466),(310,409,360,466))]:
 for side,r in [('left',left),('right',right)]:add(name+'_'+side,'05_head_parts',r,'head.ear.'+side,canvas_rect=group,pivot=[(group[2]-group[0])/2,group[3]-group[1]],note='Left/right means image side. Both files share full pair canvas and lower-center pivot; do not trim independently.')
for name,rect in [('leaves',(381,403,473,467)),('hair',(490,399,593,471)),('crown',(24,520,113,595)),('helmet',(115,512,195,599)),('hood',(204,519,299,604)),('headscarf',(306,529,387,601)),('horn_small',(422,514,471,598)),('horn_large',(530,514,577,598))]:add(name,'05_head_parts',rect,'head.'+name,pivot=[(rect[2]-rect[0])/2,rect[3]-rect[1]],note='Hood and headscarf include visible skin/cream areas already baked into source; preserve source as-is.' if name in ['hood','headscarf'] else '')
# 6: marks; split blush and individual bars, preserve grouped versions only for spots.
for name,rect in [('stripe_upper',(636,402,691,430)),('stripe_lower',(636,430,691,458)),('cross_mark',(791,400,851,459)),('king_mark',(876,400,932,459)),('mask',(789,506,858,574)),('brow_mark',(872,519,939,565))]:add(name,'06_markings',rect,'face.marking')
for name,r in [('left',(709,522,740,559)),('right',(742,522,773,559))]:add('blush_'+name,'06_markings',r,'face.blush.'+name,canvas_rect=(709,522,773,559),pivot=[32,18.5],note='Shared pair canvas; left/right means image side.')
for name,r in [('left',(715,399,743,459)),('right',(744,399,767,459))]:add('forehead_mark_'+name,'06_markings',r,'face.marking',canvas_rect=(715,399,767,459))
for name,r in [('upper',(637,514,664,542)),('middle',(661,534,685,555)),('lower',(639,545,662,570))]:add('spot_'+name,'06_markings',r,'face.marking',canvas_rect=(636,513,685,570))
# 7: body and equipment
for name,rect in [('cape',(975,400,1065,476)),('armor',(1074,399,1152,475)),('robe',(1160,399,1246,483)),('scarf',(1258,405,1338,481)),('spear',(1370,375,1418,484)),('dagger',(1456,391,1501,484)),('belt',(975,550,1062,594)),('pauldron',(1070,526,1135,600)),('backpack',(1157,526,1235,603)),('cloak',(1255,527,1333,603)),('necklace',(1349,531,1424,605)),('chest_emblem',(1445,527,1514,603))]:add(name,'07_body_equipment',rect,'equipment.'+name)
# 8: illustrative composites, explicitly not interchangeable components.
cols=[(25,126),(140,242),(261,363),(375,483),(489,595),(612,712),(720,824),(830,939),(947,1057),(1069,1182),(1186,1297),(1303,1412),(1415,1523)]
for row,(y0,y1) in enumerate([(696,817),(845,974)],1):
 for col,(x0,x1) in enumerate(cols,1):add(f'example_{row:02d}_{col:02d}','reference',(x0,y0,x1,y1),'reference','reference',note='Original example composite only. Text labels excluded; no component layers inferred.')
# Check exact pixel fidelity in each extraction region and transparent padding.
for e in entries:
 out=Image.open(root/e['file']); x,y=e['source_offset_xy'];w,h=e['source_crop_size']; assert out.crop((x,y,x+w,y+h)).tobytes()==im.crop(e['source_rect_xyxy']).tobytes()
 e['pixel_fidelity_verified']=True
# Atlas for human visual inspection. Gray background neutralizes invisible RGB noise.
font=ImageFont.load_default(); items=[e for e in entries if e['status']!='reference']; cw,ch=180,150; cols=8;rows=(len(items)+cols-1)//cols
sheet=Image.new('RGB',(cols*cw,rows*ch),'#777777');d=ImageDraw.Draw(sheet)
for i,e in enumerate(items):
 x=(i%cols)*cw;y=(i//cols)*ch; s=Image.open(root/e['file']);s.thumbnail((160,112),Image.Resampling.NEAREST);sheet.paste(s,(x+(cw-s.width)//2,y+5),s);d.text((x+4,y+122),e['id'],fill='white',font=font)
sheet.save(root/'contactsheet.png')
qa=root/'qa';qa.mkdir(exist_ok=True)
for style in ['black','white','checker']:
    sheet=Image.new('RGBA',(cols*cw,rows*ch),style if style!='checker' else '#cccccc');d=ImageDraw.Draw(sheet)
    if style=='checker':
        for y in range(0,sheet.height,12):
            for x in range(0,sheet.width,12):
                if (x//12+y//12)%2:d.rectangle((x,y,x+11,y+11),fill='#eeeeee')
    for i,e in enumerate(items):
        x=(i%cols)*cw;y=(i//cols)*ch;s=Image.open(root/e['file']);s.thumbnail((160,112),Image.Resampling.NEAREST);sheet.alpha_composite(s,(x+(cw-s.width)//2,y+5));d.text((x+4,y+122),e['id'],fill='white' if style=='black' else 'black',font=font)
    sheet.convert('RGB').save(qa/('contactsheet_'+style+'.png'))
from collections import Counter
meta={'source_filename':src.name,'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'source_size':im.size,'source_mode':'RGBA','alpha_range':im.getchannel('A').getextrema(),'alpha_histogram':dict(sorted(Counter(im.getchannel('A').getdata()).items())),'rect_convention':'[left, top, right, bottom], right/bottom exclusive, source top-left origin','counts':dict(Counter(e['section'] for e in entries)),'total_files':len(entries),'component_and_composite_files':len(items),'runtime_guidance':'Keep native canvas dimensions and pivot. Composite eye shapes cannot be independently animated as white/pupil layers. Use sclera_blank plus pupil_* for independent gaze.','assets':entries}
(root/'manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({'counts':meta['counts'],'total':len(entries),'assets':len(items),'fidelity':'all source crop RGBA bytes match'},ensure_ascii=False))
