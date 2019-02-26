# Math.Geometry.Projective
The art of synthetic projective geometry.

This projective geometry implementation focuses on the usage of synthetic methods when possible and adequate.   
So not on performance or coding per se.

Example. The stereographic projection of a 3D-sphere onto a 2D-plane is *not* implemented by a formula.   
First the spatial 3D-points on the sphere are calculated using 4 homogeneous coordinates per point.
Then the lines from the 3D-projection center to these 3D-points are calculated using 6 homogeneous coordinates per line.
Each line meets the projection plane in a 3D-point, again using 4 homogeneous coordinates per point.
These 3D-points in a plane are converted to 2D-points of a 2D projection plane using 3 homogeneous coordinates per point.
Finally the homogeneous coordinates of these 2D-points are dehomogenized, i.e. converted to affine coordinates using 2 (x,y) coordinates per point. These x-y-coordinates can be used to make the drawing.

MathNet.Numerics is extended to support homogeneous coordinates.   

The basic 1-, 2- and 3-dimensional geometric entities: points, planes, linear complexes and lines, with their meet and join operations, are modelled using homogeneous coordinates.

The project's purpose is to provide clear and simple methods to design 2- and 3-dimensional geometrical objects and configurations and to project them on a drawing surface.   

For some people it may be interesting to see how projective geometry has been used.
