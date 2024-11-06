INSERT INTO Users (UserId, Name, WalletBalance)
VALUES 
    (1, 'Mate', 500.00),
    (2, 'Luka', 300.00),
    (3, 'Ana', 150.00);

INSERT INTO Rounds (RoundId, StartTime, Status)
VALUES 
    (1, '2024-10-24 14:00:00', 'Done'),
    (2, '2024-10-24 14:03:00', 'Done'),
    (3, '2024-10-24 14:05:00', 'Done'),
    (4, '2024-10-24 14:07:00', 'Done'),
    (5, '2024-10-24 14:10:00', 'Done');

INSERT INTO Dogs (DogId, Name, RoundId)
VALUES 

    (1, 'Rocket', 1),
    (2, 'Blaze', 1),
    (3, 'Dash', 1),
    (4, 'Zoom', 1),
    (5, 'Comet', 1),
    (6, 'Ace', 1),

    (7, 'Bolt', 2),
    (8, 'Flash', 2),
    (9, 'Speedster', 2),
    (10, 'Turbo', 2),
    (11, 'Vortex', 2),
    (12, 'Jet', 2),

    (13, 'Sprint', 3),
    (14, 'Quicksilver', 3),
    (15, 'Thunder', 3),
    (16, 'Lightning', 3),
    (17, 'Hurricane', 3),
    (18, 'Cyclone', 3),

    (19, 'Blizzard', 4),
    (20, 'Typhoon', 4),
    (21, 'Twister', 4),
    (22, 'Storm', 4),
    (23, 'Gale', 4),
    (24, 'Tempest', 4),

    (25, 'Inferno', 5),
    (26, 'Fury', 5),
    (27, 'Wildfire', 5),
    (28, 'Ember', 5),
    (29, 'Scorch', 5),
    (30, 'Coal', 5);


INSERT INTO Bets (BetId, UserId, DogId, Amount, Status)
VALUES 
    (1, 1, 1, 50.00, 'Finished'),
    (2, 2, 2, 30.00, 'Finished'),
    (3, 3, 3, 20.00, 'Finished'),
    (4, 1, 4, 100.00, 'Finished'),
    (5, 2, 5, 50.00, 'Finished'),
    (6, 3, 6, 25.00, 'Finished'),

    (7, 1, 7, 60.00, 'Finished'),
    (8, 2, 8, 70.00, 'Finished'),
    (9, 3, 9, 40.00, 'Finished'),
    (10, 1, 10, 90.00, 'Finished'),
    (11, 2, 11, 80.00, 'Finished'),
    (12, 3, 12, 45.00, 'Finished'),

    (13, 1, 13, 75.00, 'Finished'),
    (14, 2, 14, 85.00, 'Finished'),
    (15, 3, 15, 55.00, 'Finished'),
    (16, 1, 16, 95.00, 'Finished'),
    (17, 2, 17, 65.00, 'Finished'),
    (18, 3, 18, 35.00, 'Finished'),

    (19, 1, 19, 90.00, 'Finished'),
    (20, 2, 20, 60.00, 'Finished'),
    (21, 3, 21, 30.00, 'Finished'),
    (22, 1, 22, 100.00, 'Finished'),
    (23, 2, 23, 50.00, 'Finished'),
    (24, 3, 24, 40.00, 'Finished'),

    (25, 1, 25, 55.00, 'Finished'),
    (26, 2, 26, 45.00, 'Finished'),
    (27, 3, 27, 35.00, 'Finished'),
    (28, 1, 28, 65.00, 'Finished'),
    (29, 2, 29, 75.00, 'Finished'),
    (30, 3, 30, 85.00, 'Finished');
