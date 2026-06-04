import bpy

# 全オブジェクトを削除
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# 黄色マテリアルを作成
mat = bpy.data.materials.new(name="YellowMaterial")
mat.use_nodes = True
bsdf = mat.node_tree.nodes["Principled BSDF"]
bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 0.0, 1.0)  # 黄色 (R,G,B,A)

# 円錐を追加
bpy.ops.mesh.primitive_cone_add(
    vertices=32,
    radius1=1.0,
    radius2=0.0,
    depth=2.0,
    location=(0, 0, 0)
)

cone = bpy.context.active_object
cone.name = "YellowCone"
cone.data.materials.append(mat)

print(f"作成完了: {cone.name}")
print(f"頂点数: {len(cone.data.vertices)}")
print(f"マテリアル: {cone.data.materials[0].name}")
