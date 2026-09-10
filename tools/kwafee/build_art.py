from pathlib import Path
import bpy, json, sys, math
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'kwaffee/Assets/KwaFee/Art'; PRE=ROOT/'.scratch/kwafee/art-preview.png'
OUT.mkdir(parents=True,exist_ok=True); PRE.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
PALETTE={'orange':(.95,.28,.07,1),'coffee':(.12,.035,.018,1),'mint':(.3,.85,.67,1),'pink':(1,.32,.47,1),'cream':(1,.78,.42,1),'blue':(.12,.48,.9,1),'white':(1,.98,.88,1),'black':(.015,.01,.008,1),'red':(.85,.08,.06,1)}
MATS={}
def mat(name,color,rough=.45,metal=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color[:3],color[3]); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=color; p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    MATS[name]=m; return m
for n,c,r in [('clay_orange','orange',.55),('plastic_orange','orange',.2),('clay_mint','mint',.6),('clay_pink','pink',.58),('clay_cream','cream',.58),('plastic_blue','blue',.22),('plastic_white','white',.18),('felt_mint','mint',.85),('coffee','coffee',.35),('ink','black',.4),('sign_red','red',.38)]: mat(n,PALETTE[c],r)
def add(o,material,asset):
    o.data.materials.append(MATS[material]); o['asset']=asset; return o
def cube(asset,loc,scale,material,bev=.06):
    bpy.ops.mesh.primitive_cube_add(location=loc); o=add(bpy.context.object,material,asset); o.name=asset; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        mod=o.modifiers.new('rounded','BEVEL'); mod.width=bev; mod.segments=3; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o
def uv(asset,loc,scale,material,seg=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=12, location=loc); o=add(bpy.context.object,material,asset); o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); [setattr(p,'use_smooth',True) for p in o.data.polygons]; return o
def cyl(asset,loc,rad,depth,material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=rad,depth=depth,location=loc); o=add(bpy.context.object,material,asset); mod=o.modifiers.new('soft','BEVEL'); mod.width=.035; mod.segments=2; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name); [setattr(p,'use_smooth',True) for p in o.data.polygons if len(p.vertices)>4]; return o
def torus(asset,loc,major,minor,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=24,minor_segments=8,location=loc,rotation=rot); o=add(bpy.context.object,material,asset); [setattr(p,'use_smooth',True) for p in o.data.polygons]; return o
def join(asset,objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join(); objs[0].name=asset
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True); return objs[0]
def cup():
    a='Cup'; o=cyl(a,(0,0,.2),.2,.4,'plastic_orange'); bpy.ops.mesh.primitive_torus_add(major_radius=.145,minor_radius=.027,major_segments=24,minor_segments=8,location=(0,0,.4)); rim=add(bpy.context.object,'plastic_orange',a)
    coffee=cyl(a,(0,0,.385),.155,.018,'coffee'); h=torus(a,(.19,0,.22),.105,.035,'clay_orange', (math.pi/2,0,0)); return join(a,[o,rim,coffee,h])
def eyes(a,x,y,z): return [uv(a,(x,y,z),(.06,.045,.06),'plastic_white'),uv(a,(x,y-.042,z+.005),(.018,.012,.024),'ink')]
def human(asset,body,head,apron=False):
    a=asset; customer=asset=='Customer'; p=[uv(a,(0,0,.42 if customer else .45),(.42 if customer else .34,.2 if customer else .25,.42 if customer else .45),body),cyl(a,(0,0,.93),.28 if customer else .22,.58 if customer else .65,body),uv(a,(0,0,1.45),(.25 if customer else .3,.23 if customer else .27,.34 if customer else .3),head)]
    p += eyes(a,-.11,-.245,1.54)+eyes(a,.11,-.245,1.54)
    p += [cyl(a,(-.44 if customer else -.38,0,.95),.09,.65,body),cyl(a,(.44 if customer else .38,0,.95),.09,.65,body),cyl(a,(-.17 if customer else -.14,0,.11),.11,.22,body),cyl(a,(.17 if customer else .14,0,.11),.11,.22,body)]
    if apron:p += [cube(a,(0,-.255,.85),(.23,.025,.4),'felt_mint',.03)]
    if asset=='Customer': p += [uv(a,(0,-.278,1.43),(.12,.025,.045),'ink')]
    return join(a,p)
def sign():
    a='Sign'; base=cube(a,(0,0,2.5),(1.45,.09,.34),'sign_red',.1); base.rotation_euler[1]=-.08
    bpy.ops.object.text_add(location=(-1.15,-.12,2.43),rotation=(math.pi/2,0,0)); t=bpy.context.object; t.data.body='KWA FEE'; t.data.align_x='LEFT'; t.data.size=.38; t.data.extrude=.025; add(t,'plastic_white',a); bpy.context.view_layer.objects.active=t; t.select_set(True); bpy.ops.object.convert(target='MESH'); return join(a,[base,t])
def espresso():
    a='Espresso'; body=cube(a,(0,0,.175),(.35,.25,.175),'clay_cream',.06)
    head=cube(a,(0,.27,.3),(.12,.07,.1),'plastic_orange',.02); pfh=cube(a,(0,.315,.2),(.05,.06,.12),'plastic_white',.015)
    tray=cube(a,(0,.265,.025),(.25,.08,.025),'plastic_white',.015); gauge=uv(a,(.01,.26,.285),(.022,.022,.022),'plastic_blue')
    return join(a,[body,head,pfh,tray,gauge])
def grinder():
    a='Grinder'; base=cube(a,(0,0,.05),(.29,.29,.05),'clay_cream',.04)
    body=cyl(a,(0,0,.4),.205,.6,'plastic_blue'); hopper=uv(a,(0,0,.76),(.17,.17,.08),'plastic_white')
    lever=cube(a,(.21,0,.46),(.05,.035,.14),'plastic_orange',.015)
    return join(a,[base,body,hopper,lever])
def steam_wand():
    a='SteamWand'; base=cube(a,(0,0,.15),(.2,.075,.15),'clay_mint',.05)
    wand=cube(a,(0,0,.7),(.04,.04,.4),'plastic_white',.02); arm=cube(a,(0,.12,1.0),(.045,.16,.04),'clay_cream',.01)
    tip=uv(a,(0,.23,1.02),(.03,.03,.03),'plastic_orange')
    return join(a,[base,wand,arm,tip])
def ice_machine():
    a='IceMachine'; body=cube(a,(0,0,.25),(.3,.45,.25),'plastic_blue',.06)
    door=cube(a,(0,.32,.25),(.24,.17,.17),'plastic_white',.04)
    cubes=[cube(a,(x,.48,.25),(.045,.02,.05),'plastic_white',.012) for x in (-.12,0,.12)]
    top=cube(a,(0,0,.55),(.34,.5,.05),'clay_cream',.03)
    return join(a,[body,door]+cubes+[top])
def build():
    objs=[cup(),human('Barista','clay_mint','clay_cream',True),human('Customer','clay_pink','clay_cream'),cube('Counter',(0,0,.5),(1.5,.5,.5),'clay_cream'),cube('Floor',(0,0,-.06),(8,6,.06),'clay_cream'),cube('Wall',(0,0,2),(8,.125,2),'clay_cream'),cube('Tray',(0,0,.05),(.7,.4,.05),'plastic_blue',.05),sign(),espresso(),grinder(),steam_wand(),ice_machine()]
    # Ground every asset on the floor: the exporter bakes the rolling
    # origin-shift into the mesh, so authored origins must sit at y=0.
    # Machines (espresso/grinder/steam_wand/ice_machine) are authored with
    # min.y==0 already; the legacy 8 are recentered here.
    import mathutils
    for o in objs:
        lo_corner_z = min(v.co.z for v in o.data.vertices)
        if lo_corner_z > 0:  # floor slab and grounded assets keep their authored base; only floaters lower to y=0
            o.matrix_world = o.matrix_world @ mathutils.Matrix.Translation(mathutils.Vector((0,0,-lo_corner_z)))
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    for o in objs:
        for p in o.data.polygons:
            if len(p.vertices)==4: p.use_smooth=False
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'core.blend'))
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.export_scene.gltf(filepath=str(OUT/'core.glb'),export_format='GLB',use_selection=True)
    return objs
def manifest(objs):
    materials=[{'name':n,'color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for n,m in MATS.items()]
    meshes=[]; totalv=totalt=0
    for o in objs:
        me=o.data; me.calc_loop_triangles(); by={m.name:i for i,m in enumerate(MATS.values())}; groups={}; vs=[]; ns=[]; allco=[]
        # Emit per-loop vertices: this preserves hard edges and lets every triangle carry a material.
        for tri in me.loop_triangles:
            mat_index=by.get(me.materials[me.polygons[tri.polygon_index].material_index].name,0)
            out=groups.setdefault(mat_index,[])
            corners=list(tri.loops); corners.reverse()  # reverse winding after Blender->Unity handedness conversion
            for li in corners:
                loop=me.loops[li]; co=o.matrix_world @ me.vertices[loop.vertex_index].co; no=o.matrix_world.to_3x3() @ loop.normal; no.normalize()
                base=len(vs)//3; vs += [co.x,co.z,co.y]; ns += [no.x,no.z,no.y]; allco.append(Vector((co.x,co.z,co.y))); out.append(base)
        lo=Vector((min(v.x for v in allco),min(v.y for v in allco),min(v.z for v in allco))); hi=Vector((max(v.x for v in allco),max(v.y for v in allco),max(v.z for v in allco)))
        submeshes=[{'material':k,'triangles':v} for k,v in sorted(groups.items())]
        meshes.append({'name':o.name,'vertices':vs,'normals':ns,'submeshes':submeshes,'bounds':{'min':list(lo),'max':list(hi)}}); totalv+=len(vs)//3; totalt+=len(me.loop_triangles)
        print(f'{o.name}: vertices={len(vs)//3} triangles={len(me.loop_triangles)} bounds={list(lo)}..{list(hi)}')
    (OUT/'core-meshes.json').write_text(json.dumps({'materials':materials,'meshes':meshes},indent=2)); print(f'TOTAL vertices={totalv} triangles={totalt}')
def preview(objs):
    # Arrange a readable contact-sheet-like staging only for the preview. The
    # authored objects remain at their local origins in the exported assets.
    layout={'Cup':(-3.0,0.0,0.2),'Barista':(-1.2,0.0,0.0),'Customer':(1.2,0.0,0.0),
            'Counter':(0.0,0.0,0.5),'Floor':(0.0,0.0,-0.06),'Wall':(0.0,3.0,-1.0),
            'Tray':(3.0,0.0,0.1),'Sign':(0.0,-0.3,0.0)}
    for o in objs:
        if o.name in layout: o.location=layout[o.name]
    bpy.ops.object.camera_add(location=(8,-14,7)); cam=bpy.context.object; bpy.context.scene.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=10
    cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.light_add(type='AREA',location=(2,-4,8)); bpy.context.object.data.energy=1400; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=5
    bpy.context.scene.render.engine='BLENDER_EEVEE'; bpy.context.scene.render.resolution_x=640; bpy.context.scene.render.resolution_y=480; bpy.context.scene.render.resolution_percentage=100; bpy.context.scene.render.filepath=str(PRE); bpy.context.scene.world.color=(.04,.02,.01); bpy.ops.render.render(write_still=True)
if __name__=='__main__':
    o=build(); manifest(o)
    if '--preview' in sys.argv: preview(o)
