/************************************************************************/
/*                                                                      */
/*   svm_struct_latent_api_types.h                                      */
/*                                                                      */
/*   API type definitions for Latent SVM^struct                         */
/*                                                                      */
/*   Author: Chun-Nam Yu                                                */
/*   Date: 30.Sep.08                                                    */
/*                                                                      */
/*   This software is available for non-commercial use only. It must    */
/*   not be modified and distributed without prior permission of the    */
/*   author. The author is not responsible for implications from the    */
/*   use of this software.                                              */
/*                                                                      */
/************************************************************************/

#include "svm_light/svm_common.h"

#include <vcclr.h>

enum FEATURES {
	FT_NONE = 0,
	FT_COLOR_WEIGHT,
	FT_SHAPE_WEIGHT,
	FT_COLOR_DIFFERENCE_PAIRWISE_WEIGHT,
	FT_CONSTANT_PAIRWISE_WEIGHT,
	FT_SHAPE_SCALE_WEIGHT,
	FT_OTHER_SHAPE_FEATURES_START
};

typedef struct pattern {
  gcroot<Research::GraphBasedShapePrior::Util::Image2D<System::Drawing::Color>^> image;
  int index;
} PATTERN;

typedef struct label {
  gcroot<Research::GraphBasedShapePrior::Shape^> shape;
} LABEL;

typedef struct latent_var {
  gcroot<Research::GraphBasedShapePrior::Util::Image2D<bool>^> mask;
} LATENT_VAR;

typedef struct example {
  PATTERN x;
  LABEL y;
  LATENT_VAR h;
} EXAMPLE;

typedef struct sample {
  int n;
  EXAMPLE *examples;
} SAMPLE;


typedef struct structmodel {
  double *w;          /* pointer to the learned weights */
  MODEL  *svm_model;  /* the learned SVM model */
  long   sizePsi;     /* maximum number of weights in w */
} STRUCTMODEL;


typedef struct struct_learn_parm {
  double epsilon;              /* precision for which to solve
				  quadratic program */
  long newconstretrain;        /* number of new constraints to
				  accumulate before recomputing the QP
				  solution */
  double C;                    /* trade-off between margin and loss */
  char   custom_argv[20][1000]; /* string set with the -u command line option */
  int    custom_argc;          /* number of -u command line options */
  int    slack_norm;           /* norm to use in objective function
                                  for slack variables; 1 -> L1-norm, 
				  2 -> L2-norm */
  int    loss_type;            /* selected loss function from -r
				  command line option. Select between
				  slack rescaling (1) and margin
				  rescaling (2) */
  int    loss_function;        /* select between different loss
				  functions via -l command line
				  option */
  /* add your own variables */
  gcroot<Research::GraphBasedShapePrior::ObjectBackgroundColorModels^> color_models;
  gcroot<Research::GraphBasedShapePrior::ShapeModel^> shape_model;
} STRUCT_LEARN_PARM;

