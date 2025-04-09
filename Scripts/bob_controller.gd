class_name BobController
extends Node

func bob(headbob_frequency: float,headbob_move_amount: float ,headbob_time : float ) -> Vector3:
    return Vector3(
            cos(headbob_time * headbob_frequency * 0.5) * headbob_move_amount,
            sin(headbob_time * headbob_frequency) * headbob_move_amount,
            0
    )
