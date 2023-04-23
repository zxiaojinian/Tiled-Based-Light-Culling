# Tiled-Based-Light-Culling

//求交方式
1.frustum plane vs sphere
2.frustum AABB vs sphere
3.1+2混合
4.Spherical-sliced Cone(https://lxjk.github.io/2018/03/25/Improve-Tile-based-Light-Culling-with-Spherical-sliced-Cone.html)

//depth discontinuities
1.2.5D
2.half z
3.modified half z

//求depth bound方式
1.atomic min & max
2.parallel reduction

![image](https://user-images.githubusercontent.com/48090628/233852287-e079d5b1-8441-4ca3-8034-b9e921d00afa.png)
