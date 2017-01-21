# Math.Geometry.Projective
The art of synthetic projective geometry in C#.

The focus of this projective geometry implementation is to try and use synthetic methods.
Only when possible, adequate, to a certain extent. 
The focus is not on performance or coding per se.

Example. A stereographic projection is not implemented by a formula. It is implemented by calculating certain lines from the projection center and calculating the meeting point of these lines with the projection plane.

The library from math.net.numerics is used and extended to support homogeneous coordinates. 
The basic 1-, 2- and 3-dimensional geometric entities: points, planes, linear complexes and lines, with their meet and join operations, are modelled using homogeneous coordinates.

The project's purpose is to provide clear and simple methods to design 2- and 3-dimensional geometrical objects and configurations and to project them on a drawing surface. For some people it may be interesting to see how projective geometry has been used.
