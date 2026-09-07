"""Bake locally owned Quake II MD5 models to MD3, preserving classic frame/skin indices.
MD3 data uses the requested .md2 filename: Q2RTX dispatches by file magic.
No game logic, map or world texture is included in the output PKZ.
"""
import argparse, hashlib, json, re, struct, zipfile, posixpath
from pathlib import Path, PurePosixPath
import numpy as np

class Pak:
    def __init__(self, path):
        self.f = open(path, 'rb')
        magic, offset, size = struct.unpack('<4sii', self.f.read(12))
        assert magic == b'PACK' and size % 64 == 0
        self.f.seek(offset)
        self.entries = {}
        for _ in range(size // 64):
            name, pos, length = struct.unpack('<56sii', self.f.read(64))
            self.entries[name.split(b'\0')[0].decode().lower()] = (pos, length)
    def read(self, name):
        pos, size = self.entries[name.lower()]
        self.f.seek(pos)
        return self.f.read(size)

def block(text, name):
    return re.search(r'\b' + name + r'\s*\{(.*?)\}', text, re.S).group(1)
def number(text, name):
    return int(re.search(r'\b' + name + r'\s+(\d+)', text).group(1))
def floats(text):
    return np.fromstring(text.replace('(', ' ').replace(')', ' '), sep=' ')
def quaternion(xyz):
    return np.r_[xyz, -np.sqrt(max(0., 1. - np.dot(xyz, xyz)))]
def rotate(q, v):
    return v + 2 * np.cross(q[..., :3], np.cross(q[..., :3], v) + q[..., 3, None] * v)
def multiply(a,b):
    return np.r_[a[3]*b[:3]+b[3]*a[:3]+np.cross(a[:3],b[:3]), a[3]*b[3]-np.dot(a[:3],b[:3])]
def fixed(text, size):
    data = text.encode('ascii')
    assert len(data) < size, text
    return data.ljust(size,b'\0')
def md2_info(data):
    h = struct.unpack_from('<17i',data)
    assert data[:4] == b'IDP2' and h[1] == 8
    skins = [data[h[11]+i*64:h[11]+(i+1)*64].split(b'\0')[0].decode().lower() for i in range(h[5])]
    names = [data[h[14]+i*h[4]+24:h[14]+i*h[4]+40].split(b'\0')[0].decode() for i in range(h[10])]
    return h, skins, names

def convert(mesh_text, anim_text, scale_data, frame_count, skins, frame_names):
    mesh_text = re.sub(r'//[^\n]*','',mesh_text)
    anim_text = re.sub(r'//[^\n]*','',anim_text)
    nj = number(anim_text,'numJoints')
    assert nj == number(mesh_text,'numJoints')
    nf = number(anim_text,'numFrames')
    assert nf >= frame_count, (nf,frame_count)
    hierarchy = re.findall(r'"([^"]+)"\s+(-?\d+)\s+(\d+)\s+(\d+)',block(anim_text,'hierarchy'))
    assert len(hierarchy)==nj
    base = floats(block(anim_text,'baseframe')).reshape(nj,6)
    frame_values = {int(i):floats(values) for i,values in re.findall(r'\bframe\s+(\d+)\s*\{(.*?)\}',anim_text,re.S)}
    jp=np.zeros((frame_count,nj,3)); jq=np.zeros((frame_count,nj,4)); js=np.ones((frame_count,nj))
    for f in range(frame_count):
        for j,(name,parent,flags,start) in enumerate(hierarchy):
            parent,flags,start=int(parent),int(flags),int(start)
            components=base[j].copy(); cursor=start
            for k in range(6):
                if flags & (1<<k): components[k]=frame_values[f][cursor]; cursor+=1
            sc=scale_data.get(name,{})
            js[f,j]=float(sc.get(str(f),1))
            p=components[:3] * (js[f,j] if sc.get('scale_positions',False) else 1)
            q=quaternion(components[3:])
            if parent>=0:
                assert parent<j
                p=jp[f,parent]+rotate(jq[f,parent],p)
                q=multiply(jq[f,parent],q); q/=np.linalg.norm(q)
            jp[f,j]=p; jq[f,j]=q
    surfaces=[]; all_bounds=[]; stats=[]
    for mi,mb in enumerate(re.findall(r'\bmesh\s*\{(.*?)\}',mesh_text,re.S)):
        verts=re.findall(r'\bvert\s+(\d+)\s*\(\s*([^)]*)\)\s+(\d+)\s+(\d+)',mb)
        tris=np.array([[int(a),int(b),int(c)] for _,a,b,c in re.findall(r'\btri\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)',mb)],dtype=np.int32)
        weights=re.findall(r'\bweight\s+(\d+)\s+(\d+)\s+([^\s]+)\s*\(\s*([^)]*)\)',mb)
        assert len(verts)==number(mb,'numverts') and len(weights)==number(mb,'numweights')
        nv=len(verts); uv=np.zeros((nv,2)); owner=np.full(len(weights),-1,dtype=int)
        for i,uvstr,start,count in verts:
            i,start,count=int(i),int(start),int(count); uv[i]=floats(uvstr); owner[start:start+count]=i
        assert (owner>=0).all() and tris.min()>=0 and tris.max()<nv
        wj=np.array([int(w[1]) for w in weights]); wb=np.array([float(w[2]) for w in weights]); wp=np.array([floats(w[3]) for w in weights])
        sums=np.bincount(owner,weights=wb,minlength=nv); assert np.max(abs(sums-1))<0.002
        positions=[]; packed=[]
        for f in range(frame_count):
            posed=(jp[f,wj]+rotate(jq[f,wj],wp)*js[f,wj,None])*wb[:,None]
            pos=np.zeros((nv,3)); np.add.at(pos,owner,posed)
            assert np.isfinite(pos).all()
            qpos=np.rint(pos*64)
            assert qpos.min()>=-32768 and qpos.max()<=32767, 'MD3 coordinate overflow'
            # MD5 clockwise triangles: outward normal is (v2-v0) cross (v1-v0).
            normals=np.zeros_like(pos)
            face=np.cross(pos[tris[:,2]]-pos[tris[:,0]],pos[tris[:,1]]-pos[tris[:,0]])
            for corner in range(3): np.add.at(normals,tris[:,corner],face)
            lengths=np.linalg.norm(normals,axis=1); normals/=np.maximum(lengths[:,None],1e-20)
            lat=np.rint(np.arccos(np.clip(normals[:,2],-1,1))*255/(2*np.pi)).astype(np.uint8)
            lng=(np.rint(np.arctan2(normals[:,1],normals[:,0])*255/(2*np.pi)).astype(int)%256).astype(np.uint8)
            arr=np.zeros(nv,dtype=[('xyz','<i2',(3,)),('normal','u1',(2,))]); arr['xyz']=qpos; arr['normal'][:,0]=lat; arr['normal'][:,1]=lng
            packed.append(arr.tobytes()); positions.append([pos.min(axis=0),pos.max(axis=0)])
        all_bounds.append(np.array(positions)); stats.append({'vertices':nv,'triangles':len(tris)})
        tri_bytes=tris.astype('<i4').tobytes(); skin_bytes=b''.join(fixed(s,64)+struct.pack('<i',i) for i,s in enumerate(skins)); uv_bytes=uv.astype('<f4').tobytes(); vertex_bytes=b''.join(packed)
        ot=108; os=ot+len(tri_bytes); ou=os+len(skin_bytes); ov=ou+len(uv_bytes); end=ov+len(vertex_bytes)
        header=struct.pack('<4s64s10i',b'IDP3',fixed('mesh'+str(mi),64),0,frame_count,len(skins),nv,len(tris),ot,os,ou,ov,end)
        surfaces.append(header+tri_bytes+skin_bytes+uv_bytes+vertex_bytes)
    bounds=np.array(all_bounds); mins=bounds[:,:,0,:].min(axis=0); maxs=bounds[:,:,1,:].max(axis=0)
    frames=b''.join(struct.pack('<10f16s',*mins[f],*maxs[f],0,0,0,float(np.linalg.norm(np.maximum(abs(mins[f]),abs(maxs[f])))),fixed(frame_names[f][:15],16)) for f in range(frame_count))
    offset=108+len(frames); total=offset+sum(map(len,surfaces))
    header=struct.pack('<4si64s9i',b'IDP3',15,fixed('Quake II remaster baked',64),0,frame_count,0,len(surfaces),0,108,offset,offset,total)
    return header+frames+b''.join(surfaces), {'frames':frame_count,'remaster_frames':nf,'meshes':stats,'frame0_bounds':[mins[0].tolist(),maxs[0].tolist()]}

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--remaster',required=True); ap.add_argument('--classic',required=True); ap.add_argument('--output',required=True); a=ap.parse_args()
    rem=Pak(a.remaster); old=Pak(a.classic); output=Path(a.output); report={'converted':[],'skipped':[]}; textures={}
    with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for path in sorted(old.entries):
            if not path.endswith('.md2') or not path.startswith(('models/','players/')): continue
            p=PurePosixPath(path); stem=str(p.parent/'md5'/p.stem)
            if stem+'.md5mesh' not in rem.entries: continue
            try:
                h,oldskins,names=md2_info(old.read(path)); skins=[]
                for skin_index,skin in enumerate(oldskins):
                    sp=PurePosixPath(posixpath.normpath(skin)); candidate=str(sp.parent/'md5'/sp.with_suffix('.png').name)
                    if candidate not in rem.entries:
                        # The remaster moved tank commander skins into tank/cskin,cpain.
                        remskins=md2_info(rem.read(path))[1]
                        sp=PurePosixPath(posixpath.normpath(remskins[skin_index]))
                        candidate=str(sp.parent/'md5'/sp.with_suffix('.png').name)
                    if candidate not in rem.entries: raise ValueError('Missing remaster skin '+candidate)
                    skins.append(candidate)
                if not skins: raise ValueError('No classic skin mapping')
                scales=json.loads(rem.read(stem+'.md5scale')) if stem+'.md5scale' in rem.entries else {}
                blob,stats=convert(rem.read(stem+'.md5mesh').decode(),rem.read(stem+'.md5anim').decode(),scales,h[10],skins,names)
                z.writestr(path,blob)
                for skin in skins: textures[skin]=rem.read(skin)
                stats.update(path=path,skins=skins,classic_vertices=h[6],sha256=hashlib.sha256(blob).hexdigest())
                report['converted'].append(stats); print(path,stats['meshes'],flush=True)
            except Exception as e:
                report['skipped'].append({'path':path,'reason':str(e)}); print('SKIP',path,str(e),flush=True)
        for path,data in textures.items(): z.writestr(path,data)
    report['textures']=len(textures); report['package_sha256']=hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix('.json').write_text(json.dumps(report,indent=2))
    print('Converted',len(report['converted']),'skipped',len(report['skipped']),'textures',len(textures))
if __name__=='__main__': main()
