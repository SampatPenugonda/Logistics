# Logistics Solutions <br />

# Latest Modifications <br />
1. Removed the dependencies of POJO Classes at DAL Layer. 
2. Improved the usage of constants across the solutions.
3. Enhanced the exception handling throughout the application. 
4. Maintained the Read/Write Preferences for collection[s]
5. Added PlaneHistory with schema version to handle collections  for multiple versions through changeStream.
6. Made Dal Layer more independent by creating service layer. 
7. Added Global Exception handling by using custom middleware.


# Indexes
1. Created Indexes on Collections City & Plane for GeoSphere 2D columns. 
2. Index creation will be handled in the program during application start-up 

# Developed & Tested the solution using MongoDb Atlas.

# New MongoDB Featues : $geoNear , $Near

$**geoNear** is used to calculate the distance travelled in planeDal.cs & Another way of distance calculation is implemented in Change Stream. 

$Near is used to calculate Neighbor cities.

# Schema Changes 
1. Added Schema Version for Cargo Collection, Plane History collection to maintain the Schema Changes, Since Cargo , plane history Collection is highly used. 

2. Added Few Fields in to Plane Collection to maintain distanceTravelled , TimeTravelledinSeconds & Maintainance Required Flags. 
3. Above fields will be added / modified using the changeStream. 

# Logging
1. Default logging is applied through out the code. 
2. Logging the custom message with various Id's 

# Testing
Tested the solution with 200 planes & 2000+ cities as mentioned. 

# Scripts: 

1. Scripts to generate 2000+ cities 

db.worldcities.aggregate([{ $match: { population: { $gt: '1000' } } }, { $sort: { population: -1 } }, { $group: { _id: '$country', cities: { $ROOT' } } }, { $project: { _id: '$_id', cities15: { $slice: [ '$cities', 15 ] } } }, { $unwind: { path: '$cities15', preserveNullAndEmptyArrays: false } }, { $project: { _id: { $concat: [ { $replaceAll: { input: '$cities15.country', find: '/', replacement: '%2F' } }, ' - ', { $replaceAll: { input: '$cities15.city_ascii', find: '/', replacement: '%2F' } } ] }, position: [ '$cities15.lng', '$cities15.lat' ], country: '$cities15.country' } }, { $out: 'cities' }])

2. Script to generate 200 planes : 

first = { $sample: { size: 200} } second = { $group: { _id: null, planes : { $push : { currentLocation : "$position" }}}} unwind = { $unwind : {path: "$planes", includeArrayIndex: "id" }} format = {$project : { _id : {$concat : ["CARGO",{$toString:"$id"}]}, currentLocation: "$planes.currentLocation", heading:{$literal:0}, route: []}} asplanes = { $out: "planes"} db.cities.aggregate([firstN,second,unwind,format,asplanes])

# Running the Code

open localhost:5000 to verify the Loaded UI & run the test harness to check delivered parcels. 
